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
            return new ResolvedTemplateSource(null, expression, allowRewrite: false);
        }

        if (!LiteralSpanMapper.TryMap(model, literal, cancellationToken, out var constMap))
        {
            return new ResolvedTemplateSource(null, expression, allowRewrite: false);
        }

        var containingType = constSymbol.ContainingType;
        if (containingType is null ||
            !SymbolEqualityComparer.Default.Equals(containingType, loggingMethod.ContainingType))
        {
            return new ResolvedTemplateSource(constMap, literal, allowRewrite: false);
        }

        if (!exclusivityCache.TryGetValue(constSymbol, out var exclusive))
        {
            exclusive = IsExclusiveToMethod(model.Compilation, constSymbol, loggingMethod, cancellationToken);
            exclusivityCache.TryAdd(constSymbol, exclusive);
        }

        return new ResolvedTemplateSource(constMap, literal, exclusive && constMap.AllowRewrite);
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
        Compilation compilation,
        ISymbol constSymbol,
        ISymbol loggingMethod,
        CancellationToken cancellationToken)
    {
        var containingType = constSymbol.ContainingType;
        if (containingType is null ||
            !SymbolEqualityComparer.Default.Equals(containingType, loggingMethod.ContainingType))
        {
            return false;
        }

        var uses = 0;
        foreach (var syntaxRef in containingType.DeclaringSyntaxReferences)
        {
            var tree = syntaxRef.SyntaxTree;
            var root = syntaxRef.GetSyntax(cancellationToken);
            var model = compilation.GetSemanticModel(tree);
            foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
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
        }

        return uses == 1;
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
