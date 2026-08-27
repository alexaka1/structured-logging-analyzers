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
        bool allowRewrite,
        IReadOnlyList<ExpressionSyntax?>? argumentExpressions = null,
        bool uniquifyDuplicates = false)
    {
        AnalyzeDuplicates(
            context,
            map,
            named,
            skipHole,
            allowRewrite,
            settings,
            argumentExpressions,
            uniquifyDuplicates);
        AnalyzeNaming(context, map, named, settings, regexCache, skipHole, allowRewrite);
    }

    public static void AnalyzePositional(
        SyntaxNodeAnalysisContext context,
        TemplateSourceMap map,
        PropertyHole[] positional,
        IReadOnlyList<IParameterSymbol>? templateParameters,
        IReadOnlyList<ExpressionSyntax?>? argumentExpressions,
        AnalyzerSettings settings,
        bool allowRewrite)
    {
        var style = settings.GetNaming(DiagnosticIds.InconsistentTemplatePropertyNaming);
        string?[]? leafNames = null;
        string?[]? qualifiedNames = null;
        if (allowRewrite && templateParameters is not null && templateParameters.Count == positional.Length)
        {
            leafNames = SuggestFromParameters(positional.Length, templateParameters, style);
        }
        else if (allowRewrite && argumentExpressions is not null)
        {
            SuggestFromArguments(positional.Length, argumentExpressions, style, out leafNames, out qualifiedNames);
        }

        for (var i = 0; i < positional.Length; i++)
        {
            var hole = positional[i];
            ImmutableDictionary<string, string?>? properties = null;
            var leaf = leafNames?[i];
            if (!string.IsNullOrEmpty(leaf))
            {
                properties = ImmutableDictionary<string, string?>.Empty
                    .Add(FixProperties.SuggestedName, leaf)
                    .Add(FixProperties.PropertyName, hole.PropertyName)
                    .Add(FixProperties.NameLogicalStart, hole.NameStartIndex.ToString(CultureInfo.InvariantCulture))
                    .Add(FixProperties.NameLogicalLength, hole.NameLength.ToString(CultureInfo.InvariantCulture))
                    .Add(FixProperties.AllowRewrite, "true");
                var qualified = qualifiedNames?[i];
                if (!string.IsNullOrEmpty(qualified) &&
                    !string.Equals(qualified, leaf, StringComparison.Ordinal))
                {
                    properties = properties.Add(FixProperties.QualifiedSuggestedName, qualified);
                }
            }

            ReportHole(context, map, hole, Descriptors.PositionalPropertyUsed, properties);
        }
    }

    private static string?[] SuggestFromParameters(
        int count,
        IReadOnlyList<IParameterSymbol> templateParameters,
        PropertyNamingStyle style)
    {
        var names = new string?[count];
        var used = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            var suggested = PropertyNaming.Suggest(templateParameters[i].Name, style);
            if (!string.IsNullOrEmpty(suggested) && !ExpressionPropertyName.IsPositionalName(suggested))
            {
                names[i] = ExpressionPropertyName.Uniquify(suggested, used);
            }
        }

        return names;
    }

    private static void SuggestFromArguments(
        int count,
        IReadOnlyList<ExpressionSyntax?> argumentExpressions,
        PropertyNamingStyle style,
        out string?[] leafNames,
        out string?[] qualifiedNames)
    {
        leafNames = new string?[count];
        qualifiedNames = new string?[count];
        var usedLeaf = new HashSet<string>(StringComparer.Ordinal);
        var usedQualified = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            var expression = i < argumentExpressions.Count ? argumentExpressions[i] : null;
            if (expression is null)
            {
                continue;
            }

            var leaf = ExpressionPropertyName.TrySuggest(expression, style, ExpressionPropertyName.Kind.Leaf);
            if (!string.IsNullOrEmpty(leaf))
            {
                leafNames[i] = ExpressionPropertyName.Uniquify(leaf!, usedLeaf);
            }

            var qualified = ExpressionPropertyName.TrySuggest(expression, style, ExpressionPropertyName.Kind.Qualified);
            if (!string.IsNullOrEmpty(qualified))
            {
                qualifiedNames[i] = ExpressionPropertyName.Uniquify(qualified!, usedQualified);
            }
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
        bool allowRewrite,
        AnalyzerSettings settings,
        IReadOnlyList<ExpressionSyntax?>? argumentExpressions,
        bool uniquifyDuplicates)
    {
        var duplicate = new bool[named.Length];
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

            duplicate[i] = count > 1;
        }

        var style = settings.GetNaming(DiagnosticIds.InconsistentTemplatePropertyNaming);
        string?[]? leafNames = null;
        string?[]? qualifiedNames = null;
        if (allowRewrite && uniquifyDuplicates)
        {
            SuggestDuplicateNames(
                named,
                skipHole,
                duplicate,
                argumentExpressions,
                style,
                out leafNames,
                out qualifiedNames);
        }

        for (var i = 0; i < named.Length; i++)
        {
            if (!duplicate[i])
            {
                continue;
            }

            ImmutableDictionary<string, string?>? properties = null;
            if (allowRewrite && uniquifyDuplicates)
            {
                properties = DuplicateFixProperties(named[i], leafNames?[i], qualifiedNames?[i]);
            }
            else if (!allowRewrite)
            {
                properties = ImmutableDictionary<string, string?>.Empty.Add(FixProperties.AllowRewrite, "false");
            }

            ReportHole(context, map, named[i], Descriptors.DuplicateTemplateProperty, properties);
        }
    }

    private static ImmutableDictionary<string, string?>? DuplicateFixProperties(
        PropertyHole hole,
        string? leaf,
        string? qualified)
    {
        var current = hole.PropertyName;
        var primary = !string.IsNullOrEmpty(leaf) && !string.Equals(leaf, current, StringComparison.Ordinal)
            ? leaf
            : !string.IsNullOrEmpty(qualified) && !string.Equals(qualified, current, StringComparison.Ordinal)
                ? qualified
                : null;
        if (primary is null)
        {
            return null;
        }

        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(FixProperties.SuggestedName, primary)
            .Add(FixProperties.PropertyName, current)
            .Add(FixProperties.NameLogicalStart, hole.NameStartIndex.ToString(CultureInfo.InvariantCulture))
            .Add(FixProperties.NameLogicalLength, hole.NameLength.ToString(CultureInfo.InvariantCulture))
            .Add(FixProperties.AllowRewrite, "true");
        if (!string.IsNullOrEmpty(leaf) &&
            !string.IsNullOrEmpty(qualified) &&
            !string.Equals(qualified, leaf, StringComparison.Ordinal) &&
            !string.Equals(qualified, current, StringComparison.Ordinal) &&
            !string.Equals(primary, qualified, StringComparison.Ordinal))
        {
            properties = properties.Add(FixProperties.QualifiedSuggestedName, qualified);
        }

        return properties;
    }

    private static void SuggestDuplicateNames(
        PropertyHole[] named,
        Func<PropertyHole, bool>? skipHole,
        bool[] duplicate,
        IReadOnlyList<ExpressionSyntax?>? argumentExpressions,
        PropertyNamingStyle style,
        out string?[] leafNames,
        out string?[] qualifiedNames)
    {
        leafNames = new string?[named.Length];
        qualifiedNames = new string?[named.Length];
        var usedLeaf = new HashSet<string>(StringComparer.Ordinal);
        var usedQualified = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < named.Length; i++)
        {
            if (skipHole?.Invoke(named[i]) == true)
            {
                usedLeaf.Add(named[i].PropertyName);
                usedQualified.Add(named[i].PropertyName);
                continue;
            }

            if (!duplicate[i])
            {
                usedLeaf.Add(named[i].PropertyName);
                usedQualified.Add(named[i].PropertyName);
                continue;
            }

            var expression = argumentExpressions != null && i < argumentExpressions.Count
                ? argumentExpressions[i]
                : null;
            var leaf = SuggestDuplicateName(named[i], expression, style, ExpressionPropertyName.Kind.Leaf);
            leafNames[i] = ExpressionPropertyName.Uniquify(leaf, usedLeaf);

            var qualified = SuggestDuplicateName(named[i], expression, style, ExpressionPropertyName.Kind.Qualified);
            qualifiedNames[i] = ExpressionPropertyName.Uniquify(qualified, usedQualified);
        }
    }

    private static string SuggestDuplicateName(
        PropertyHole hole,
        ExpressionSyntax? expression,
        PropertyNamingStyle style,
        ExpressionPropertyName.Kind kind)
    {
        if (expression is not null)
        {
            var fromArgument = ExpressionPropertyName.TrySuggest(expression, style, kind);
            if (!string.IsNullOrEmpty(fromArgument))
            {
                return fromArgument!;
            }
        }

        var fromHole = PropertyNaming.Suggest(hole.PropertyName, style);
        return string.IsNullOrEmpty(fromHole) ? hole.PropertyName : fromHole;
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
