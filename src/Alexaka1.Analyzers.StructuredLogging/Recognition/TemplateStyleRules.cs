using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Alexaka1.Analyzers.StructuredLogging.Classification;
using Alexaka1.Analyzers.StructuredLogging.Configuration;
using Alexaka1.Analyzers.StructuredLogging.Mapping;
using Alexaka1.Analyzers.StructuredLogging.Parsing;

namespace Alexaka1.Analyzers.StructuredLogging.Recognition;

internal static class TemplateStyleRules
{
    public static void AnalyzeNamed(
        SyntaxNodeAnalysisContext context,
        TemplateSourceMap map,
        PropertyHole[] named,
        AnalyzerSettings settings,
        RegexCache regexCache,
        Func<PropertyHole, bool>? skipHole,
        bool allowRewrite)
    {
        AnalyzeDuplicates(context, map, named, skipHole, allowRewrite);
        AnalyzeNaming(context, map, named, settings, regexCache, skipHole, allowRewrite);
    }

    public static void AnalyzePositional(
        SyntaxNodeAnalysisContext context,
        TemplateSourceMap map,
        PropertyHole[] positional,
        IReadOnlyList<IParameterSymbol>? templateParameters,
        AnalyzerSettings settings,
        bool allowRewrite)
    {
        var canRename = allowRewrite &&
                        templateParameters is not null &&
                        templateParameters.Count == positional.Length;
        for (var i = 0; i < positional.Length; i++)
        {
            var hole = positional[i];
            ImmutableDictionary<string, string?>? properties = null;
            if (canRename)
            {
                var suggested = PropertyNaming.Suggest(
                    templateParameters![i].Name,
                    settings.GetNaming(DiagnosticIds.InconsistentTemplatePropertyNaming));
                if (!string.IsNullOrEmpty(suggested))
                {
                    properties = ImmutableDictionary<string, string?>.Empty
                        .Add(FixProperties.SuggestedName, suggested)
                        .Add(FixProperties.PropertyName, hole.PropertyName)
                        .Add(FixProperties.NameLogicalStart, hole.NameStartIndex.ToString(CultureInfo.InvariantCulture))
                        .Add(FixProperties.NameLogicalLength, hole.NameLength.ToString(CultureInfo.InvariantCulture))
                        .Add(FixProperties.AllowRewrite, "true");
                }
            }

            ReportHole(context, map, hole, Descriptors.PositionalPropertyUsed, properties);
        }
    }

    public static void AnalyzeTrailingPeriod(
        SyntaxNodeAnalysisContext context,
        SemanticModel model,
        ExpressionSyntax expression,
        TemplateSourceMap? preferredMap,
        bool allowRewrite,
        CancellationToken cancellationToken)
    {
        var last = LastFragment(expression);
        var map = preferredMap;
        if (map is null || !ReferenceEquals(map.Expression, last))
        {
            if (!LiteralSpanMapper.TryMap(model, last, cancellationToken, out var fragmentMap))
            {
                return;
            }

            map = fragmentMap;
        }

        var text = map.Value;
        if (text.Length == 0 || text[text.Length - 1] != '.')
        {
            return;
        }

        if (text.Length > 1 && text[text.Length - 2] == '.')
        {
            return;
        }

        var span = map.TryGetSpan(text.Length - 1, 1) ?? last.Span;
        var properties = allowRewrite
            ? ImmutableDictionary<string, string?>.Empty
            : ImmutableDictionary<string, string?>.Empty.Add(FixProperties.AllowRewrite, "false");
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LogMessageIsSentence,
            Location.Create(last.SyntaxTree, span),
            properties));
    }

    private static void AnalyzeDuplicates(
        SyntaxNodeAnalysisContext context,
        TemplateSourceMap map,
        PropertyHole[] named,
        Func<PropertyHole, bool>? skipHole,
        bool allowRewrite)
    {
        _ = allowRewrite;
        for (var i = 0; i < named.Length; i++)
        {
            if (skipHole?.Invoke(named[i]) == true)
            {
                continue;
            }

            var count = 0;
            for (var j = 0; j < named.Length; j++)
            {
                if (skipHole?.Invoke(named[j]) == true)
                {
                    continue;
                }

                if (named[i].PropertyName == named[j].PropertyName)
                {
                    count++;
                }
            }

            if (count > 1)
            {
                ReportHole(context, map, named[i], Descriptors.DuplicateTemplateProperty);
            }
        }
    }

    private static void AnalyzeNaming(
        SyntaxNodeAnalysisContext context,
        TemplateSourceMap map,
        PropertyHole[] named,
        AnalyzerSettings settings,
        RegexCache regexCache,
        Func<PropertyHole, bool>? skipHole,
        bool allowRewrite)
    {
        foreach (var hole in named)
        {
            if (skipHole?.Invoke(hole) == true || string.IsNullOrEmpty(hole.PropertyName))
            {
                continue;
            }

            if (settings.IsIgnored(hole.PropertyName, regexCache, DiagnosticIds.InconsistentTemplatePropertyNaming))
            {
                continue;
            }

            var suggested = PropertyNaming.Suggest(
                hole.PropertyName,
                settings.GetNaming(DiagnosticIds.InconsistentTemplatePropertyNaming));
            if (string.Equals(suggested, hole.PropertyName, StringComparison.Ordinal))
            {
                continue;
            }

            var properties = ImmutableDictionary<string, string?>.Empty
                .Add(FixProperties.SuggestedName, suggested)
                .Add(FixProperties.PropertyName, hole.PropertyName)
                .Add(FixProperties.NameLogicalStart, hole.NameStartIndex.ToString(CultureInfo.InvariantCulture))
                .Add(FixProperties.NameLogicalLength, hole.NameLength.ToString(CultureInfo.InvariantCulture))
                .Add(FixProperties.AllowRewrite, allowRewrite ? "true" : "false");

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InconsistentTemplatePropertyNaming,
                HoleLocation(map, hole),
                properties,
                hole.PropertyName,
                suggested));
        }
    }

    private static void ReportHole(
        SyntaxNodeAnalysisContext context,
        TemplateSourceMap map,
        PropertyHole hole,
        DiagnosticDescriptor descriptor,
        ImmutableDictionary<string, string?>? properties = null)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            HoleLocation(map, hole),
            properties ?? ImmutableDictionary<string, string?>.Empty));
    }

    internal static Location HoleLocation(TemplateSourceMap map, PropertyHole hole)
    {
        var tree = map.Expression.SyntaxTree;
        var span = map.TryGetSpan(hole.StartIndex, hole.Length);
        if (span is null)
        {
            return Location.Create(tree, map.Expression.Span);
        }

        return Location.Create(tree, span.Value);
    }

    internal static ExpressionSyntax LastFragment(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;
                case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression):
                    expression = binary.Right;
                    continue;
                default:
                    return expression;
            }
        }
    }
}
