// Copyright (c) 2026 alexaka1

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using StructuredLogging.Analyzers;
using StructuredLogging.Analyzers.Mapping;

namespace StructuredLogging.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddDestructuringCodeFixProvider))]
[Shared]
public sealed class AddDestructuringCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        DiagnosticIds.AnonymousObjectMustBeDestructured,
        DiagnosticIds.ComplexObjectShouldBeDestructured);

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
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Add destructuring to property",
                    ct => ApplyAsync(context.Document, diagnostic, ct),
                    nameof(AddDestructuringCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var insertAt = await ResolveInsertPositionAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
        if (insertAt is null)
        {
            return document;
        }

        var updated = text.WithChanges(new TextChange(new TextSpan(insertAt.Value, 0), "@"));
        return document.WithText(updated);
    }

    private static async Task<int?> ResolveInsertPositionAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        if (diagnostic.Properties.TryGetValue(FixProperties.InsertLogicalIndex, out var logicalText) &&
            int.TryParse(logicalText, out var logicalIndex))
        {
            var mapped = await TryMapContainingTemplateAsync(document, diagnostic.Location.SourceSpan, cancellationToken)
                .ConfigureAwait(false);
            var source = mapped?.TryGetSourceStart(logicalIndex);
            if (source is not null)
            {
                return source;
            }
        }

        var span = diagnostic.Location.SourceSpan;
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var snippet = sourceText.ToString(span);
        var brace = snippet.IndexOf('{');
        if (brace >= 0)
        {
            return span.Start + brace + 1;
        }

        return span.Start + 1;
    }

    internal static async Task<TemplateSourceMap?> TryMapContainingTemplateAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return null;
        }

        var token = root.FindToken(span.Start);
        var attributeArgument = token.Parent?.FirstAncestorOrSelf<AttributeArgumentSyntax>();
        var argument = token.Parent?.FirstAncestorOrSelf<ArgumentSyntax>();
        var expression = attributeArgument?.Expression ?? argument?.Expression ?? token.Parent?.FirstAncestorOrSelf<ExpressionSyntax>();
        if (expression is null)
        {
            return null;
        }

        return LiteralSpanMapper.TryMap(model, expression, cancellationToken, out var map) ? map : null;
    }
}
