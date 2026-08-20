// Copyright (c) 2026 alexaka1

namespace StructuredLogging.Analyzers.Parsing;

internal sealed class ParsedTemplate
{
    public static ParsedTemplate Empty { get; } = new(
        string.Empty,
        Array.Empty<PropertyHole>(),
        namedProperties: null,
        positionalProperties: null,
        isMixed: false);

    public ParsedTemplate(
        string text,
        PropertyHole[] properties,
        PropertyHole[]? namedProperties,
        PropertyHole[]? positionalProperties,
        bool isMixed)
    {
        Text = text;
        Properties = properties;
        NamedProperties = namedProperties;
        PositionalProperties = positionalProperties;
        IsMixed = isMixed;
    }

    public string Text { get; }

    public PropertyHole[] Properties { get; }

    /// <summary>
    /// Named (or mixed) holes. Null when the template is empty of holes or all-positional.
    /// </summary>
    public PropertyHole[]? NamedProperties { get; }

    /// <summary>
    /// Positional holes when every hole is positional. Null otherwise.
    /// </summary>
    public PropertyHole[]? PositionalProperties { get; }

    public bool IsMixed { get; }
}
