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

    public AnalyzerSettings(PropertyNamingStyle naming, string? ignoredPattern)
    {
        Naming = naming;
        IgnoredPattern = ignoredPattern;
    }

    public static AnalyzerSettings Default { get; } = new(PropertyNamingStyle.PascalCase, ignoredPattern: null);

    public PropertyNamingStyle Naming { get; }

    public string? IgnoredPattern { get; }

    public static AnalyzerSettings From(AnalyzerConfigOptionsProvider provider, SyntaxTree tree)
    {
        var options = provider.GetOptions(tree);
        var naming = PropertyNamingStyle.PascalCase;
        if (TryGetOption(options, NamingOptionName, out var namingValue))
        {
            naming = ParseNaming(namingValue);
        }

        string? pattern = null;
        if (TryGetOption(options, IgnoredRegexOptionName, out var regexValue))
        {
            pattern = regexValue;
        }

        return new AnalyzerSettings(naming, pattern);
    }

    public bool IsIgnored(string propertyName, RegexCache cache)
    {
        if (string.IsNullOrEmpty(IgnoredPattern))
        {
            return false;
        }

        var regex = cache.Get(IgnoredPattern!);
        return regex != null && regex.IsMatch(propertyName);
    }

    private static bool TryGetOption(AnalyzerConfigOptions options, string optionName, out string value)
    {
        if (TryGetScoped(options, DiagnosticIds.Prefix, optionName, out value) ||
            TryGetScoped(options, DiagnosticIds.InconsistentTemplatePropertyNaming, optionName, out value) ||
            TryGetScoped(options, DiagnosticIds.InconsistentContextPropertyNaming, optionName, out value))
        {
            return true;
        }

        value = null!;
        return false;
    }

    private static bool TryGetScoped(
        AnalyzerConfigOptions options,
        string scope,
        string optionName,
        out string value)
    {
        if (options.TryGetValue("dotnet_code_quality." + scope + "." + optionName, out value) &&
            !string.IsNullOrWhiteSpace(value))
        {
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
            default:
                return PropertyNamingStyle.PascalCase;
        }
    }
}
