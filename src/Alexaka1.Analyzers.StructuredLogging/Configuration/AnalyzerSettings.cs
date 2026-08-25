// Copyright (c) 2026 alexaka1

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Alexaka1.Analyzers.StructuredLogging.Classification;

namespace Alexaka1.Analyzers.StructuredLogging.Configuration;

internal readonly struct AnalyzerSettings
{
    public const string NamingOptionName = "property_naming";
    public const string IgnoredRegexOptionName = "ignored_properties_regex";

    public const string NamingKey = "dotnet_code_quality." + DiagnosticIds.Prefix + "." + NamingOptionName;
    public const string IgnoredRegexKey = "dotnet_code_quality." + DiagnosticIds.Prefix + "." + IgnoredRegexOptionName;

    private readonly PropertyNamingStyle? _prefixNaming;
    private readonly PropertyNamingStyle? _templateNaming;
    private readonly PropertyNamingStyle? _contextNaming;
    private readonly string? _prefixIgnored;
    private readonly string? _templateIgnored;
    private readonly string? _contextIgnored;

    private AnalyzerSettings(
        PropertyNamingStyle? prefixNaming,
        PropertyNamingStyle? templateNaming,
        PropertyNamingStyle? contextNaming,
        string? prefixIgnored,
        string? templateIgnored,
        string? contextIgnored)
    {
        _prefixNaming = prefixNaming;
        _templateNaming = templateNaming;
        _contextNaming = contextNaming;
        _prefixIgnored = prefixIgnored;
        _templateIgnored = templateIgnored;
        _contextIgnored = contextIgnored;
    }

    public static AnalyzerSettings From(AnalyzerConfigOptionsProvider provider, SyntaxTree tree)
    {
        var options = provider.GetOptions(tree);
        return new AnalyzerSettings(
            TryGetNaming(options, DiagnosticIds.Prefix),
            TryGetNaming(options, DiagnosticIds.InconsistentTemplatePropertyNaming),
            TryGetNaming(options, DiagnosticIds.InconsistentContextPropertyNaming),
            TryGetIgnored(options, DiagnosticIds.Prefix),
            TryGetIgnored(options, DiagnosticIds.InconsistentTemplatePropertyNaming),
            TryGetIgnored(options, DiagnosticIds.InconsistentContextPropertyNaming));
    }

    public PropertyNamingStyle GetNaming(string diagnosticId)
    {
        if (_prefixNaming is { } prefix)
        {
            return prefix;
        }

        if (diagnosticId == DiagnosticIds.InconsistentContextPropertyNaming)
        {
            return _contextNaming ?? PropertyNamingStyle.PascalCase;
        }

        return _templateNaming ?? PropertyNamingStyle.PascalCase;
    }

    public bool TemplateNamingIsSemanticConventions =>
        GetNaming(DiagnosticIds.InconsistentTemplatePropertyNaming) == PropertyNamingStyle.SemanticConventions;

    public bool IsIgnored(string propertyName, RegexCache cache, string diagnosticId)
    {
        var pattern = _prefixIgnored;
        if (string.IsNullOrEmpty(pattern))
        {
            pattern = diagnosticId == DiagnosticIds.InconsistentContextPropertyNaming
                ? _contextIgnored
                : _templateIgnored;
        }

        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        var regex = cache.Get(pattern!);
        return regex != null && regex.IsMatch(propertyName);
    }

    private static PropertyNamingStyle? TryGetNaming(AnalyzerConfigOptions options, string scope)
    {
        if (TryGetScoped(options, scope, NamingOptionName, out var value))
        {
            return ParseNaming(value);
        }

        return null;
    }

    private static string? TryGetIgnored(AnalyzerConfigOptions options, string scope)
    {
        if (TryGetScoped(options, scope, IgnoredRegexOptionName, out var value))
        {
            return value;
        }

        return null;
    }

    private static bool TryGetScoped(
        AnalyzerConfigOptions options,
        string scope,
        string optionName,
        out string value)
    {
        if (options.TryGetValue("dotnet_code_quality." + scope + "." + optionName, out var raw) &&
            !string.IsNullOrWhiteSpace(raw))
        {
            value = raw;
            return true;
        }

        value = null!;
        return false;
    }

    private static PropertyNamingStyle ParseNaming(string value)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "camel_case":
            case "camelcase":
                return PropertyNamingStyle.CamelCase;
            case "snake_case":
            case "snakecase":
                return PropertyNamingStyle.SnakeCase;
            case "elastic_naming":
            case "elastic":
                return PropertyNamingStyle.ElasticNaming;
            case "semantic_conventions":
            case "semantic_convention":
            case "semconv":
                return PropertyNamingStyle.SemanticConventions;
            default:
                return PropertyNamingStyle.PascalCase;
        }
    }
}
