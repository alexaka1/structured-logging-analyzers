using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alexaka1.Analyzers.StructuredLogging.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReplaceContextualLoggerTypeCodeFixProvider))]
[Shared]
public sealed class ReplaceContextualLoggerTypeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.ContextualLoggerMismatch);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!TryGetReplacement(root, model, diagnostic.Location.SourceSpan, context.CancellationToken, out _, out _, out var containingName))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Use {containingName} as the logger category",
                    ct => ApplyAsync(context.Document, diagnostic, ct),
                    nameof(ReplaceContextualLoggerTypeCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return document;
        }

        if (!TryGetReplacement(
                root,
                model,
                diagnostic.Location.SourceSpan,
                cancellationToken,
                out var replacements,
                out _,
                out _))
        {
            return document;
        }

        var updated = root.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);
        return document.WithSyntaxRoot(updated);
    }

    private static bool TryGetReplacement(
        SyntaxNode root,
        SemanticModel model,
        Microsoft.CodeAnalysis.Text.TextSpan span,
        CancellationToken cancellationToken,
        out Dictionary<TypeSyntax, TypeSyntax> replacements,
        out INamedTypeSymbol containing,
        out string containingName)
    {
        replacements = new Dictionary<TypeSyntax, TypeSyntax>();
        containing = null!;
        containingName = string.Empty;

        var node = root.FindNode(span, getInnermostNodeForTie: true);
        var containingType = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (containingType is null)
        {
            return false;
        }

        var containingSymbol = model.GetDeclaredSymbol(containingType, cancellationToken);
        if (containingSymbol is not INamedTypeSymbol containingNamed)
        {
            return false;
        }

        containing = containingNamed;
        containingName = containingNamed.ToMinimalDisplayString(model, span.Start);
        if (string.IsNullOrEmpty(containingName))
        {
            return false;
        }

        var invocation = node as InvocationExpressionSyntax ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation is not null &&
            span.Start >= invocation.SpanStart &&
            span.End <= invocation.Span.End &&
            IsForContextInvocation(invocation))
        {
            var typeArgument = GetSingleTypeArgument(invocation);
            if (typeArgument is null)
            {
                return false;
            }

            replacements[typeArgument] = ParseType(containingName, typeArgument);
            return true;
        }

        var reportedType = node as TypeSyntax ?? node.FirstAncestorOrSelf<TypeSyntax>();
        if (reportedType is null || !TryGetLoggerCategory(model, reportedType, cancellationToken, out var wrongCategory))
        {
            return false;
        }

        foreach (var typeSyntax in LoggerCategoryTypes(containingType))
        {
            if (!TryGetLoggerCategory(model, typeSyntax, cancellationToken, out var category) ||
                !SymbolEqualityComparer.Default.Equals(category, wrongCategory))
            {
                continue;
            }

            var argument = GetLoggerTypeArgument(typeSyntax);
            if (argument is null)
            {
                continue;
            }

            replacements[argument] = ParseType(containingName, argument);
        }

        return replacements.Count > 0;
    }

    private static IEnumerable<TypeSyntax> LoggerCategoryTypes(TypeDeclarationSyntax containingType)
    {
        if (containingType.ParameterList is not null)
        {
            foreach (var parameter in containingType.ParameterList.Parameters)
            {
                if (parameter.Type is not null)
                {
                    yield return parameter.Type;
                }
            }
        }

        foreach (var member in containingType.Members)
        {
            if (member is TypeDeclarationSyntax)
            {
                continue;
            }

            switch (member)
            {
                case ConstructorDeclarationSyntax constructor:
                    foreach (var parameter in constructor.ParameterList.Parameters)
                    {
                        if (parameter.Type is not null)
                        {
                            yield return parameter.Type;
                        }
                    }

                    break;
                case FieldDeclarationSyntax field:
                    yield return field.Declaration.Type;
                    break;
                case PropertyDeclarationSyntax property:
                    yield return property.Type;
                    break;
            }
        }
    }

    private static bool IsForContextInvocation(InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } => genericName.Identifier.ValueText,
            _ => null
        };
        return name == "ForContext";
    }

    private static TypeSyntax? GetSingleTypeArgument(InvocationExpressionSyntax invocation)
    {
        var typeArguments = invocation.Expression switch
        {
            GenericNameSyntax generic => generic.TypeArgumentList,
            MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } => genericName.TypeArgumentList,
            _ => null
        };
        return typeArguments is { Arguments.Count: 1 } ? typeArguments.Arguments[0] : null;
    }

    private static bool TryGetLoggerCategory(
        SemanticModel model,
        TypeSyntax type,
        CancellationToken cancellationToken,
        out ITypeSymbol category)
    {
        category = null!;
        var symbol = model.GetTypeInfo(UnwrapNullable(type), cancellationToken).Type as INamedTypeSymbol;
        if (symbol is null || symbol.TypeArguments.Length != 1)
        {
            return false;
        }

        if (symbol.OriginalDefinition.MetadataName != "ILogger`1" ||
            symbol.ContainingNamespace?.ToDisplayString() != "Microsoft.Extensions.Logging")
        {
            return false;
        }

        category = symbol.TypeArguments[0];
        return true;
    }

    private static TypeSyntax? GetLoggerTypeArgument(TypeSyntax type)
    {
        var current = UnwrapNullable(type);
        while (current is QualifiedNameSyntax qualified)
        {
            current = qualified.Right;
        }

        if (current is AliasQualifiedNameSyntax alias)
        {
            current = alias.Name;
        }

        return current is GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } generic
            ? generic.TypeArgumentList.Arguments[0]
            : null;
    }

    private static TypeSyntax UnwrapNullable(TypeSyntax type) =>
        type is NullableTypeSyntax nullable ? nullable.ElementType : type;

    private static TypeSyntax ParseType(string display, TypeSyntax original) =>
        SyntaxFactory.ParseTypeName(display).WithTriviaFrom(original);
}
