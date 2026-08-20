// Copyright (c) 2026 alexaka1

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Alexaka1.Analyzers.StructuredLogging.Classification;

namespace Alexaka1.Analyzers.StructuredLogging.Configuration;

internal readonly struct AnalyzerSettings
{
    public const string NamingKey = "structured_logging_property_naming";
    public const string IgnoredRegexKey = "structured_logging_ignored_properties_regex";

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
        if (options.TryGetValue(NamingKey, out var namingValue))
        {
            naming = ParseNaming(namingValue);
        }

        string? pattern = null;
        if (options.TryGetValue(IgnoredRegexKey, out var regexValue) && !string.IsNullOrWhiteSpace(regexValue))
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
