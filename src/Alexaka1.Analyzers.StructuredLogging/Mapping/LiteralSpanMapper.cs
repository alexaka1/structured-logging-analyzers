using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Alexaka1.Analyzers.StructuredLogging.Mapping;

internal readonly struct MappedChar
{
    public MappedChar(TextSpan sourceSpan)
    {
        SourceSpan = sourceSpan;
    }

    public TextSpan SourceSpan { get; }
}

internal sealed class TemplateSourceMap
{
    public TemplateSourceMap(string value, MappedChar[] map, ExpressionSyntax expression)
    {
        Value = value;
        Map = map;
        Expression = expression;
    }

    public string Value { get; }

    public MappedChar[] Map { get; }

    public ExpressionSyntax Expression { get; }

    public TextSpan? TryGetSpan(int logicalStart, int length)
    {
        if (logicalStart < 0 || length <= 0 || logicalStart >= Map.Length)
        {
            return null;
        }

        var endIndex = logicalStart + length - 1;
        if (endIndex >= Map.Length)
        {
            endIndex = Map.Length - 1;
        }

        var start = Map[logicalStart].SourceSpan.Start;
        var end = Map[endIndex].SourceSpan.End;
        if (end < start)
        {
            return null;
        }

        return TextSpan.FromBounds(start, end);
    }

    public int? TryGetSourceStart(int logicalIndex)
    {
        if (logicalIndex < 0 || logicalIndex >= Map.Length)
        {
            return null;
        }

        return Map[logicalIndex].SourceSpan.Start;
    }
}

internal static class LiteralSpanMapper
{
    public static bool TryMap(SemanticModel model, ExpressionSyntax expression, CancellationToken cancellationToken, out TemplateSourceMap map)
    {
        var fragments = new List<(string Value, MappedChar[] Chars)>();
        if (!TryCollect(model, expression, fragments, cancellationToken))
        {
            map = null!;
            return false;
        }

        var total = 0;
        for (var i = 0; i < fragments.Count; i++)
        {
            total += fragments[i].Value.Length;
        }

        var builder = new MappedChar[total];
        var text = new char[total];
        var offset = 0;
        for (var i = 0; i < fragments.Count; i++)
        {
            var fragment = fragments[i];
            fragment.Value.CopyTo(0, text, offset, fragment.Value.Length);
            Array.Copy(fragment.Chars, 0, builder, offset, fragment.Chars.Length);
            offset += fragment.Value.Length;
        }

        map = new TemplateSourceMap(new string(text), builder, expression);
        return true;
    }

    public static bool TryGetConstantText(SemanticModel model, ExpressionSyntax expression, CancellationToken cancellationToken, out string text)
    {
        var constant = model.GetConstantValue(expression, cancellationToken);
        if (constant.HasValue && constant.Value is string s)
        {
            text = s;
            return true;
        }

        text = null!;
        return false;
    }

    private static bool TryCollect(
        SemanticModel model,
        ExpressionSyntax expression,
        List<(string, MappedChar[])> fragments,
        CancellationToken cancellationToken)
    {
        expression = Unwrap(expression);

        if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
        {
            return TryCollect(model, binary.Left, fragments, cancellationToken) &&
                   TryCollect(model, binary.Right, fragments, cancellationToken);
        }

        if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return TryMapLiteral(literal.Token, fragments);
        }

        if (expression is InterpolatedStringExpressionSyntax interpolated)
        {
            return TryMapConstantInterpolation(model, interpolated, fragments, cancellationToken);
        }

        return false;
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;
                case CastExpressionSyntax cast:
                    expression = cast.Expression;
                    continue;
                default:
                    return expression;
            }
        }
    }

    private static bool TryMapLiteral(SyntaxToken token, List<(string, MappedChar[])> fragments)
    {
        var value = token.ValueText;
        var source = token.Text;
        var spanStart = token.SpanStart;
        MappedChar[] map;

        if (IsRaw(source))
        {
            if (!TryMapRaw(source, spanStart, value, out map))
            {
                return false;
            }
        }
        else if (IsVerbatim(source))
        {
            map = MapVerbatim(source, spanStart, value);
        }
        else
        {
            map = MapRegular(source, spanStart, value);
        }

        if (map.Length != value.Length)
        {
            map = FallbackMap(token.Span, value);
        }

        fragments.Add((value, map));
        return true;
    }

    private static bool TryMapConstantInterpolation(
        SemanticModel model,
        InterpolatedStringExpressionSyntax interpolated,
        List<(string, MappedChar[])> fragments,
        CancellationToken cancellationToken)
    {
        var constant = model.GetConstantValue(interpolated, cancellationToken);
        if (!constant.HasValue || constant.Value is not string)
        {
            return false;
        }

        foreach (var content in interpolated.Contents)
        {
            if (content is InterpolatedStringTextSyntax text)
            {
                var value = text.TextToken.ValueText;
                var map = FallbackMap(text.TextToken.Span, value);
                fragments.Add((value, map));
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsVerbatim(string source)
    {
        return source.Length >= 2 && source[0] == '@' && source[1] == '"';
    }

    private static bool IsRaw(string source)
    {
        var i = 0;
        if (i < source.Length && source[i] == '@')
        {
            i++;
        }

        if (i + 2 >= source.Length || source[i] != '"' || source[i + 1] != '"' || source[i + 2] != '"')
        {
            return false;
        }

        return true;
    }

    private static MappedChar[] MapRegular(string source, int spanStart, string value)
    {
        var map = new MappedChar[value.Length];
        var logical = 0;
        var i = 0;
        if (i < source.Length && source[i] == 'u')
        {
            i++;
        }

        if (i < source.Length && source[i] == '8')
        {
            i++;
        }

        if (i < source.Length && source[i] == '"')
        {
            i++;
        }

        while (i < source.Length && logical < value.Length)
        {
            if (source[i] == '"' && i == source.Length - 1)
            {
                break;
            }

            if (source[i] == '\\' && i + 1 < source.Length)
            {
                var escapeStart = i;
                i++;
                var consumed = ReadEscape(source, ref i, out var logicalChars);
                var span = new TextSpan(spanStart + escapeStart, i - escapeStart);
                for (var n = 0; n < logicalChars && logical < value.Length; n++)
                {
                    map[logical++] = new MappedChar(span);
                }

                if (consumed)
                {
                    continue;
                }
            }

            map[logical++] = new MappedChar(new TextSpan(spanStart + i, 1));
            i++;
        }

        return map;
    }

    private static bool ReadEscape(string source, ref int i, out int logicalChars)
    {
        logicalChars = 1;
        if (i >= source.Length)
        {
            return false;
        }

        var c = source[i];
        i++;
        switch (c)
        {
            case '\'':
            case '"':
            case '\\':
            case '0':
            case 'a':
            case 'b':
            case 'f':
            case 'n':
            case 'r':
            case 't':
            case 'v':
                return true;
            case 'x':
                ReadHex(source, ref i, 1, 4);
                return true;
            case 'u':
                ReadHex(source, ref i, 4, 4);
                return true;
            case 'U':
                var code = ReadHex(source, ref i, 8, 8);
                if (code > 0xFFFF)
                {
                    logicalChars = 2;
                }

                return true;
            default:
                return true;
        }
    }

    private static int ReadHex(string source, ref int i, int min, int max)
    {
        var value = 0;
        var taken = 0;
        while (taken < max && i < source.Length && IsHex(source[i]))
        {
            value = (value << 4) + HexValue(source[i]);
            i++;
            taken++;
        }

        _ = min;
        return value;
    }

    private static bool IsHex(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }

    private static int HexValue(char c)
    {
        if (c >= '0' && c <= '9')
        {
            return c - '0';
        }

        if (c >= 'a' && c <= 'f')
        {
            return c - 'a' + 10;
        }

        return c - 'A' + 10;
    }

    private static MappedChar[] MapVerbatim(string source, int spanStart, string value)
    {
        var map = new MappedChar[value.Length];
        var i = 0;
        if (i < source.Length && source[i] == '@')
        {
            i++;
        }

        if (i < source.Length && source[i] == '"')
        {
            i++;
        }

        var logical = 0;
        while (i < source.Length && logical < value.Length)
        {
            if (source[i] == '"')
            {
                if (i + 1 < source.Length && source[i + 1] == '"')
                {
                    map[logical++] = new MappedChar(new TextSpan(spanStart + i, 2));
                    i += 2;
                    continue;
                }

                break;
            }

            map[logical++] = new MappedChar(new TextSpan(spanStart + i, 1));
            i++;
        }

        return map;
    }

    private static bool TryMapRaw(string source, int spanStart, string value, out MappedChar[] map)
    {
        var i = 0;
        if (i < source.Length && source[i] == '@')
        {
            i++;
        }

        var quoteCount = 0;
        while (i < source.Length && source[i] == '"')
        {
            quoteCount++;
            i++;
        }

        if (quoteCount < 3)
        {
            map = Array.Empty<MappedChar>();
            return false;
        }

        if (i < source.Length && source[i] == '\r')
        {
            i++;
        }

        if (i < source.Length && source[i] == '\n')
        {
            i++;
        }

        map = new MappedChar[value.Length];
        for (var logical = 0; logical < value.Length && i < source.Length; logical++, i++)
        {
            map[logical] = new MappedChar(new TextSpan(spanStart + i, 1));
        }

        return map.Length == value.Length;
    }

    private static MappedChar[] FallbackMap(TextSpan span, string value)
    {
        var map = new MappedChar[value.Length];
        var unit = new MappedChar(span);
        for (var i = 0; i < map.Length; i++)
        {
            map[i] = unit;
        }

        return map;
    }
}
