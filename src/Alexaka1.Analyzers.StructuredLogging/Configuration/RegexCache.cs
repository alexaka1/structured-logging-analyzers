using System.Text.RegularExpressions;

namespace Alexaka1.Analyzers.StructuredLogging.Configuration;

/// <summary>
/// Per-compilation cache for user-supplied ignore regexes and their match results.
/// Invalid patterns and patterns that time out match nothing for the rest of the compilation.
/// </summary>
internal sealed class RegexCache
{
    private readonly ConcurrentDictionary<string, PatternMatcher> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Tries to match with a pattern that compiled successfully. A pattern disabled after a
    /// match timeout remains usable for precedence and returns a non-match.
    /// </summary>
    public bool TryMatch(string pattern, string propertyName, out bool isMatch)
    {
        var matcher = _cache.GetOrAdd(pattern, static value => new PatternMatcher(value));
        if (!matcher.CompiledSuccessfully)
        {
            isMatch = false;
            return false;
        }

        isMatch = matcher.IsMatch(propertyName);
        return true;
    }

    private sealed class PatternMatcher
    {
        private readonly ConcurrentDictionary<string, bool> _results = new(StringComparer.Ordinal);
        private Regex? _regex;

        public bool CompiledSuccessfully { get; }

        public PatternMatcher(string pattern)
        {
            try
            {
                _regex = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                CompiledSuccessfully = true;
            }
            catch (ArgumentException)
            {
                _regex = null;
                CompiledSuccessfully = false;
            }
        }

        public bool IsMatch(string propertyName)
        {
            var regex = Volatile.Read(ref _regex);
            if (regex is null)
            {
                return false;
            }

            var result = _results.GetOrAdd(propertyName, name => Match(regex, name));
            return Volatile.Read(ref _regex) is not null && result;
        }

        private bool Match(Regex regex, string propertyName)
        {
            if (Volatile.Read(ref _regex) is null)
            {
                return false;
            }

            try
            {
                return regex.IsMatch(propertyName);
            }
            catch (RegexMatchTimeoutException)
            {
                Interlocked.CompareExchange(ref _regex, null, regex);
                return false;
            }
        }
    }
}
