using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alexaka1.Analyzers.StructuredLogging.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddContextDestructuringCodeFixProvider))]
[Shared]
public sealed class AddContextDestructuringCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.ComplexObjectInContextShouldBeDestructured);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var invocation = FindInvocation(root, diagnostic.Location.SourceSpan);
            if (invocation is null || invocation.ArgumentList.Arguments.Count != 2)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Destructure context property",
                    ct => ApplyAsync(context.Document, diagnostic, ct),
                    nameof(AddContextDestructuringCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var invocation = FindInvocation(root, diagnostic.Location.SourceSpan);
        if (invocation is null || invocation.ArgumentList.Arguments.Count != 2)
        {
            return document;
        }

        var destructure = SyntaxFactory.Argument(
                SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("destructureObjects")),
                default,
                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))
            .WithLeadingTrivia(SyntaxFactory.Space);
        var updated = invocation.WithArgumentList(invocation.ArgumentList.AddArguments(destructure));
        return document.WithSyntaxRoot(root.ReplaceNode(invocation, updated));
    }

    private static InvocationExpressionSyntax? FindInvocation(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var node = root.FindNode(span, getInnermostNodeForTie: true);
        return node as InvocationExpressionSyntax ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
    }
}
