// Copyright (c) 2026 alexaka1

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Alexaka1.Analyzers.StructuredLogging.Configuration;

/// <summary>
/// Per-compilation cache for user-supplied ignore regexes. Invalid patterns are
/// treated as matching nothing so the analyzer never throws.
/// </summary>
internal sealed class RegexCache
{
    private readonly ConcurrentDictionary<string, Regex?> _cache = new(StringComparer.Ordinal);

    public Regex? Get(string pattern)
    {
        return _cache.GetOrAdd(pattern, Create);
    }

    private static Regex? Create(string pattern)
    {
        try
        {
            return new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }
}
