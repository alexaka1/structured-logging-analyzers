using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alexaka1.Analyzers.StructuredLogging.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameContextPropertyCodeFixProvider))]
[Shared]
public sealed class RenameContextPropertyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.InconsistentContextPropertyNaming);

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
            if (!diagnostic.Properties.TryGetValue(FixProperties.SuggestedName, out var suggested) ||
                string.IsNullOrEmpty(suggested))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Rename property to '{suggested}'",
                    ct => ApplyAsync(context.Document, root, diagnostic, suggested!, ct),
                    nameof(RenameContextPropertyCodeFixProvider)),
                diagnostic);
        }
    }

    private static Task<Document> ApplyAsync(
        Document document,
        SyntaxNode root,
        Diagnostic diagnostic,
        string suggested,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var argument = node.FirstAncestorOrSelf<ArgumentSyntax>() ?? node as ArgumentSyntax;
        if (argument?.Expression is not LiteralExpressionSyntax literal ||
            !literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return Task.FromResult(document);
        }

        var replacement = literal.WithToken(SyntaxFactory.Literal(suggested));
        var updated = root.ReplaceNode(literal, replacement);
        return Task.FromResult(document.WithSyntaxRoot(updated));
    }
}
