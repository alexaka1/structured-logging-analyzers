// Copyright (c) 2026 alexaka1

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Alexaka1.Analyzers.StructuredLogging;

namespace Alexaka1.Analyzers.StructuredLogging.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveTrailingPeriodCodeFixProvider))]
[Shared]
public sealed class RemoveTrailingPeriodCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.LogMessageIsSentence);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Properties.TryGetValue(FixProperties.AllowRewrite, out var allow) &&
                string.Equals(allow, "false", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Remove period",
                    ct => ApplyAsync(context.Document, diagnostic, ct),
                    nameof(RemoveTrailingPeriodCodeFixProvider)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> ApplyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var span = diagnostic.Location.SourceSpan;
        if (span.Length == 0)
        {
            return document;
        }

        return document.WithText(text.WithChanges(new TextChange(span, string.Empty)));
    }
}
