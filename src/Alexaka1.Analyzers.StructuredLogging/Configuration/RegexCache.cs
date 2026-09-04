using System.Text.RegularExpressions;

namespace Alexaka1.Analyzers.StructuredLogging.Configuration;

/// <summary>
/// Per-compilation cache for user-supplied ignore regexes and their match results.
/// Invalid patterns and patterns that time out match nothing for the rest of the compilation.
/// </summary>
internal sealed class RegexCache
{
    private readonly ConcurrentDictionary<string, PatternMatcher> _cache = new(StringComparer.Ordinal);

    public bool IsMatch(string pattern, string propertyName)
    {
        return _cache.GetOrAdd(pattern, static value => new PatternMatcher(value)).IsMatch(propertyName);
    }

    private sealed class PatternMatcher
    {
        private readonly ConcurrentDictionary<string, bool> _results = new(StringComparer.Ordinal);
        private Regex? _regex;

        public PatternMatcher(string pattern)
        {
            try
            {
                _regex = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            }
            catch (ArgumentException)
            {
                _regex = null;
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
