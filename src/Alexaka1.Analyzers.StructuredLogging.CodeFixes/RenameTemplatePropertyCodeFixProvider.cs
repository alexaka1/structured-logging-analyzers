// Copyright (c) 2026 alexaka1

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Alexaka1.Analyzers.StructuredLogging.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameTemplatePropertyCodeFixProvider))]
[Shared]
public sealed class RenameTemplatePropertyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DiagnosticIds.InconsistentTemplatePropertyNaming,
            DiagnosticIds.PositionalPropertyUsed);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(FixProperties.SuggestedName, out var suggested) ||
                string.IsNullOrEmpty(suggested))
            {
                continue;
            }

            if (diagnostic.Properties.TryGetValue(FixProperties.AllowRewrite, out var allow) &&
                string.Equals(allow, "false", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Rename property to '{suggested}'",
                    ct => ApplyAsync(context.Document, diagnostic, suggested!, ct),
                    nameof(RenameTemplatePropertyCodeFixProvider) + suggested),
                diagnostic);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task<Document> ApplyAsync(
        Document document,
        Diagnostic diagnostic,
        string suggested,
        CancellationToken cancellationToken)
    {
        if (!diagnostic.Properties.TryGetValue(FixProperties.NameLogicalStart, out var startText) ||
            !diagnostic.Properties.TryGetValue(FixProperties.NameLogicalLength, out var lengthText) ||
            !int.TryParse(startText, out var logicalStart) ||
            !int.TryParse(lengthText, out var logicalLength))
        {
            return document;
        }

        var map = await AddDestructuringCodeFixProvider
            .TryMapContainingTemplateAsync(document, diagnostic.Location.SourceSpan, cancellationToken)
            .ConfigureAwait(false);
        var span = map?.TryGetSpan(logicalStart, logicalLength);
        if (span is null)
        {
            return document;
        }

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return document.WithText(text.WithChanges(new TextChange(span.Value, suggested)));
    }
}
