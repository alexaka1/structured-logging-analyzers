namespace Alexaka1.Analyzers.StructuredLogging.Parsing;

/// <summary>
/// Parses the shared message-template grammar documented in
/// docs/compatibility.md. See PROVENANCE.md for implementation lineage.
/// </summary>
internal static class MessageTemplateParser
{
    public static ParsedTemplate Parse(string template)
    {
        if (template is null)
        {
            throw new ArgumentNullException(nameof(template));
        }

        if (template.Length == 0)
        {
            return ParsedTemplate.Empty;
        }

        List<PropertyHole>? holes = null;
        var index = 0;
        while (index < template.Length)
        {
            var open = FindUnescapedOpenBrace(template, index);
            if (open < 0)
            {
                break;
            }

            if (TryReadHole(template, open, out var hole, out var consumedThrough))
            {
                (holes ??= new List<PropertyHole>()).Add(hole);
                index = consumedThrough;
                continue;
            }

            // A failed hole may still contain a later `{`; resume there instead of
            // treating the rest of the failed span as text. `{Value:{Good}}` stays
            // one hole because that outer parse succeeds.
            var nested = FindUnescapedOpenBrace(template, open + 1);
            index = nested >= 0 && nested < consumedThrough ? nested : consumedThrough;
        }

        return holes is null ? ParsedTemplate.Empty : Classify(holes);
    }

    private static int FindUnescapedOpenBrace(string template, int start)
    {
        var i = start;
        while (i < template.Length)
        {
            var c = template[i];
            if (c == '{')
            {
                if (i + 1 < template.Length && template[i + 1] == '{')
                {
                    i += 2;
                    continue;
                }

                return i;
            }

            if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
            {
                i += 2;
                continue;
            }

            i++;
        }

        return -1;
    }

    private static bool TryReadHole(string template, int open, out PropertyHole hole, out int next)
    {
        hole = default;
        var scan = open + 1;
        while (scan < template.Length && template[scan] != '}')
        {
            scan++;
        }

        if (scan >= template.Length || template[scan] != '}')
        {
            next = scan;
            return false;
        }

        next = scan + 1;
        var raw = template.Substring(open, next - open);
        var interior = raw.Substring(1, raw.Length - 2);
        if (interior.Length == 0)
        {
            return false;
        }

        if (!TrySplitInterior(interior, out var nameAndHint, out var alignment, out var format))
        {
            return false;
        }

        var destructuring = DestructuringKind.Default;
        var name = nameAndHint;
        if (name.Length > 0 && TryReadDestructuring(name[0], out destructuring))
        {
            name = name.Substring(1);
        }

        if (name.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < name.Length; i++)
        {
            if (!IsNameChar(name[i]))
            {
                return false;
            }
        }

        if (alignment != null)
        {
            var digitStart = alignment.Length > 0 && alignment[0] == '-' ? 1 : 0;
            if (digitStart == alignment.Length)
            {
                return false;
            }

            for (var i = digitStart; i < alignment.Length; i++)
            {
                var ch = alignment[i];
                if (ch < '0' || ch > '9')
                {
                    return false;
                }
            }
        }

        var positional = int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var position) &&
                         position >= 0;
        var nameOffsetInRaw = 1 + (destructuring == DestructuringKind.Default ? 0 : 1);
        hole = new PropertyHole(
            name,
            raw,
            open,
            open + nameOffsetInRaw,
            name.Length,
            destructuring,
            format,
            alignment,
            positional);
        return true;
    }

    private static bool TrySplitInterior(string interior, out string nameAndHint, out string? alignment,
        out string? format)
    {
        var colon = interior.IndexOf(':');
        var comma = interior.IndexOf(',');
        if (colon < 0 && comma < 0)
        {
            nameAndHint = interior;
            alignment = null;
            format = null;
            return true;
        }

        if (comma < 0 || (colon >= 0 && comma > colon))
        {
            nameAndHint = interior.Substring(0, colon);
            format = colon == interior.Length - 1 ? null : interior.Substring(colon + 1);
            alignment = null;
            return true;
        }

        nameAndHint = interior.Substring(0, comma);
        if (colon < 0)
        {
            if (comma == interior.Length - 1)
            {
                alignment = format = null;
                return false;
            }

            format = null;
            alignment = interior.Substring(comma + 1);
            return true;
        }

        if (comma == colon - 1)
        {
            alignment = format = null;
            return false;
        }

        alignment = interior.Substring(comma + 1, colon - comma - 1);
        format = colon == interior.Length - 1 ? null : interior.Substring(colon + 1);
        return true;
    }

    private static bool TryReadDestructuring(char c, out DestructuringKind kind)
    {
        switch (c)
        {
            case '@':
                kind = DestructuringKind.Destructure;
                return true;
            case '$':
                kind = DestructuringKind.Stringify;
                return true;
            default:
                kind = DestructuringKind.Default;
                return false;
        }
    }

    private static bool IsNameChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == ' ';
    }

    private static ParsedTemplate Classify(List<PropertyHole> holes)
    {
        var properties = holes.ToArray();
        var allPositional = true;
        var anyPositional = false;
        for (var i = 0; i < properties.Length; i++)
        {
            if (properties[i].IsPositional)
            {
                anyPositional = true;
            }
            else
            {
                allPositional = false;
            }
        }

        if (allPositional)
        {
            return new ParsedTemplate(properties, namedProperties: null, positionalProperties: properties,
                isMixed: false);
        }

        return new ParsedTemplate(
            properties,
            namedProperties: properties,
            positionalProperties: null,
            isMixed: anyPositional);
    }
}
