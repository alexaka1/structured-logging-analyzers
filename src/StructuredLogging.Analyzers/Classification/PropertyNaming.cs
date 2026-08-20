// Copyright (c) 2026 alexaka1

using System.Text;

namespace StructuredLogging.Analyzers.Classification;

internal static class PropertyNaming
{
    public static string Suggest(string propertyName, PropertyNamingStyle style)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }

        var words = SplitWords(propertyName);
        if (words.Count == 0)
        {
            return propertyName;
        }

        switch (style)
        {
            case PropertyNamingStyle.CamelCase:
                return ToCamel(words);
            case PropertyNamingStyle.SnakeCase:
                return Join(words, '_');
            case PropertyNamingStyle.ElasticNaming:
                return Join(words, '.');
            default:
                return ToPascal(words);
        }
    }

    public static string SuggestFromExpression(string raw, PropertyNamingStyle style)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return Suggest("Value", style);
        }

        var trimmed = raw;
        if (trimmed.StartsWith("Get", StringComparison.Ordinal) && trimmed.Length > 3 && char.IsUpper(trimmed[3]))
        {
            trimmed = trimmed.Substring(3);
        }

        return Suggest(trimmed, style);
    }

    private static string ToPascal(List<string> words)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < words.Count; i++)
        {
            AppendCased(builder, words[i], upperFirst: true);
        }

        return builder.ToString();
    }

    private static string ToCamel(List<string> words)
    {
        var pascal = ToPascal(words);
        if (pascal.Length == 0)
        {
            return pascal;
        }

        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }

    private static string Join(List<string> words, char separator)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < words.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(separator);
            }

            var word = words[i];
            for (var j = 0; j < word.Length; j++)
            {
                builder.Append(char.ToLowerInvariant(word[j]));
            }
        }

        return builder.ToString();
    }

    private static void AppendCased(StringBuilder builder, string word, bool upperFirst)
    {
        if (word.Length == 0)
        {
            return;
        }

        builder.Append(upperFirst ? char.ToUpperInvariant(word[0]) : char.ToLowerInvariant(word[0]));
        for (var i = 1; i < word.Length; i++)
        {
            builder.Append(char.ToLowerInvariant(word[i]));
        }
    }

    internal static List<string> SplitWords(string name)
    {
        var words = new List<string>();
        var builder = new StringBuilder();

        void Flush()
        {
            if (builder.Length > 0)
            {
                words.Add(builder.ToString());
                builder.Clear();
            }
        }

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (!char.IsLetterOrDigit(c))
            {
                Flush();
                continue;
            }

            if (builder.Length > 0)
            {
                var prev = builder[builder.Length - 1];
                var next = i + 1 < name.Length ? name[i + 1] : '\0';
                var lowerToUpper = char.IsLower(prev) && char.IsUpper(c);
                var acronymBoundary = char.IsUpper(prev) && char.IsUpper(c) && char.IsLetter(next) && char.IsLower(next);
                if (lowerToUpper || acronymBoundary)
                {
                    Flush();
                }
            }

            builder.Append(c);
        }

        Flush();
        return words;
    }
}
