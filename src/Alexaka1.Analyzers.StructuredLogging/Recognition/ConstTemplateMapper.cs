using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Alexaka1.Analyzers.StructuredLogging.Mapping;

namespace Alexaka1.Analyzers.StructuredLogging.Recognition;

internal readonly struct ResolvedTemplateSource
{
    public ResolvedTemplateSource(TemplateSourceMap? map, ExpressionSyntax? expression, bool allowRewrite)
    {
        Map = map;
        Expression = expression;
        AllowRewrite = allowRewrite;
    }

    public TemplateSourceMap? Map { get; }

    public ExpressionSyntax? Expression { get; }

    public bool AllowRewrite { get; }
}

internal static class ConstTemplateMapper
{
    public static ResolvedTemplateSource Resolve(
        SemanticModel model,
        ExpressionSyntax? expression,
        ISymbol loggingMethod,
        ConcurrentDictionary<ISymbol, bool> exclusivityCache,
        ConcurrentDictionary<INamedTypeSymbol,
            ImmutableDictionary<string, ImmutableArray<SyntaxToken>>> identifierTokensByType,
        ConcurrentDictionary<SyntaxTree, SemanticModel> semanticModels,
        CancellationToken cancellationToken)
    {
        if (expression is null)
        {
            return new ResolvedTemplateSource(null, null, allowRewrite: false);
        }

        if (LiteralSpanMapper.TryMap(model, expression, cancellationToken, out var map))
        {
            return new ResolvedTemplateSource(map, expression, map.AllowRewrite);
        }

        if (!TryGetConstLiteral(model, expression, cancellationToken, out var constSymbol, out var literal))
        {
            return ResolveUnmappedConstant(model, expression, cancellationToken);
        }

        if (!ReferenceEquals(literal.SyntaxTree, expression.SyntaxTree))
        {
            return ResolveUnmappedConstant(model, expression, cancellationToken);
        }

        if (!LiteralSpanMapper.TryMap(model, literal, cancellationToken, out var constMap))
        {
            return ResolveUnmappedConstant(model, expression, cancellationToken);
        }

        var containingType = constSymbol.ContainingType;
        if (containingType is null ||
            !SymbolEqualityComparer.Default.Equals(containingType, loggingMethod.ContainingType))
        {
            return new ResolvedTemplateSource(constMap, literal, allowRewrite: false);
        }

        if (constSymbol.DeclaredAccessibility != Accessibility.Private)
        {
            return new ResolvedTemplateSource(constMap, literal, allowRewrite: false);
        }

        if (!exclusivityCache.TryGetValue(constSymbol, out var exclusive))
        {
            exclusive = IsExclusiveToMethod(
                model,
                constSymbol,
                loggingMethod,
                identifierTokensByType,
                semanticModels,
                cancellationToken);
            exclusivityCache.TryAdd(constSymbol, exclusive);
        }

        return new ResolvedTemplateSource(constMap, literal, exclusive && constMap.AllowRewrite);
    }

    private static ResolvedTemplateSource ResolveUnmappedConstant(
        SemanticModel model,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        if (!LiteralSpanMapper.TryGetConstantText(model, expression, cancellationToken, out var text))
        {
            return new ResolvedTemplateSource(null, expression, allowRewrite: false);
        }

        var map = new TemplateSourceMap(text, Array.Empty<MappedChar>(), expression, allowRewrite: false);
        return new ResolvedTemplateSource(map, expression, allowRewrite: false);
    }

    private static bool TryGetConstLiteral(
        SemanticModel model,
        ExpressionSyntax expression,
        CancellationToken cancellationToken,
        out ISymbol constSymbol,
        out ExpressionSyntax literal)
    {
        constSymbol = null!;
        literal = null!;
        expression = Unwrap(expression);
        var symbol = model.GetSymbolInfo(expression, cancellationToken).Symbol;
        if (symbol is not IFieldSymbol field || !field.IsConst || field.Type.SpecialType != SpecialType.System_String)
        {
            return false;
        }

        foreach (var reference in field.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax declarator ||
                declarator.Initializer?.Value is null)
            {
                continue;
            }

            constSymbol = field;
            literal = declarator.Initializer.Value;
            return true;
        }

        return false;
    }

    private static bool IsExclusiveToMethod(
        SemanticModel callerModel,
        ISymbol constSymbol,
        ISymbol loggingMethod,
        ConcurrentDictionary<INamedTypeSymbol,
            ImmutableDictionary<string, ImmutableArray<SyntaxToken>>> identifierTokensByType,
        ConcurrentDictionary<SyntaxTree, SemanticModel> semanticModels,
        CancellationToken cancellationToken)
    {
        var containingType = constSymbol.ContainingType;
        if (containingType is null ||
            !SymbolEqualityComparer.Default.Equals(containingType, loggingMethod.ContainingType))
        {
            return false;
        }

        var identifiers = identifierTokensByType.GetOrAdd(
            containingType,
            type => IndexIdentifierTokens(type, cancellationToken));
        if (!identifiers.TryGetValue(constSymbol.Name, out var candidates))
        {
            return false;
        }

        var candidateCount = 0;
        foreach (var token in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsDeclarationToken(token, constSymbol))
            {
                candidateCount++;
            }
        }

        if (candidateCount == 1)
        {
            return true;
        }

        var uses = 0;
        foreach (var token in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsDeclarationToken(token, constSymbol))
            {
                continue;
            }

            if (token.Parent is not IdentifierNameSyntax identifier)
            {
                continue;
            }

            var tree = identifier.SyntaxTree;
            var model = ReferenceEquals(tree, callerModel.SyntaxTree)
                ? callerModel
                : semanticModels.GetOrAdd(tree, t => callerModel.Compilation.GetSemanticModel(t));
            var symbol = model.GetSymbolInfo(identifier, cancellationToken).Symbol;
            if (!SymbolEqualityComparer.Default.Equals(symbol, constSymbol))
            {
                continue;
            }

            uses++;
            if (uses > 1)
            {
                return false;
            }
        }

        return uses == 1;
    }

    private static ImmutableDictionary<string, ImmutableArray<SyntaxToken>> IndexIdentifierTokens(
        INamedTypeSymbol containingType,
        CancellationToken cancellationToken)
    {
        var tokensByName = new Dictionary<string, List<SyntaxToken>>(StringComparer.Ordinal);
        foreach (var syntaxRef in containingType.DeclaringSyntaxReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = syntaxRef.GetSyntax(cancellationToken);
            foreach (var token in root.DescendantTokens())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!token.IsKind(SyntaxKind.IdentifierToken))
                {
                    continue;
                }

                if (!tokensByName.TryGetValue(token.ValueText, out var tokens))
                {
                    tokens = new List<SyntaxToken>();
                    tokensByName.Add(token.ValueText, tokens);
                }

                tokens.Add(token);
            }
        }

        var result = ImmutableDictionary.CreateBuilder<string, ImmutableArray<SyntaxToken>>(StringComparer.Ordinal);
        foreach (var pair in tokensByName)
        {
            result.Add(pair.Key, pair.Value.ToImmutableArray());
        }

        return result.ToImmutable();
    }

    private static bool IsDeclarationToken(SyntaxToken token, ISymbol constSymbol)
    {
        if (token.Parent is not VariableDeclaratorSyntax declarator)
        {
            return false;
        }

        foreach (var syntaxRef in constSymbol.DeclaringSyntaxReferences)
        {
            if (ReferenceEquals(syntaxRef.SyntaxTree, token.SyntaxTree) && syntaxRef.Span == declarator.Span)
            {
                return true;
            }
        }

        return false;
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;
                case CastExpressionSyntax cast:
                    expression = cast.Expression;
                    continue;
                default:
                    return expression;
            }
        }
    }
}
