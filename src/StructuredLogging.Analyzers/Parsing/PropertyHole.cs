// Copyright (c) 2026 alexaka1

namespace StructuredLogging.Analyzers.Parsing;

internal readonly struct PropertyHole
{
    public PropertyHole(
        string propertyName,
        string rawText,
        int startIndex,
        int nameStartIndex,
        int nameLength,
        DestructuringKind destructuring,
        string? format,
        string? alignment,
        bool isPositional)
    {
        PropertyName = propertyName;
        RawText = rawText;
        StartIndex = startIndex;
        Length = rawText.Length;
        NameStartIndex = nameStartIndex;
        NameLength = nameLength;
        Destructuring = destructuring;
        Format = format;
        Alignment = alignment;
        IsPositional = isPositional;
    }

    public string PropertyName { get; }

    public string RawText { get; }

    public int StartIndex { get; }

    public int Length { get; }

    public int NameStartIndex { get; }

    public int NameLength { get; }

    public DestructuringKind Destructuring { get; }

    public string? Format { get; }

    public string? Alignment { get; }

    public bool IsPositional { get; }
}
