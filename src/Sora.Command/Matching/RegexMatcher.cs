using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Sora.Command.Matching;

/// <summary>
///     Matches when the input matches the regex expression.
///     Caches compiled regex instances for repeated patterns.
/// </summary>
public sealed class RegexMatcher : ICommandMatcher
{
    private readonly ConcurrentDictionary<string, Regex> _cache      = new();
    private readonly Lazy<ILogger>                       _loggerLazy = new(SoraLogger.CreateLogger<RegexMatcher>);
    private          ILogger                             _logger => _loggerLazy.Value;

    /// <inheritdoc />
    public MatchType MatchType => MatchType.Regex;

    /// <inheritdoc />
    public bool IsMatch(string input, string expression)
    {
        try
        {
            Regex regex = _cache.GetOrAdd(
                expression,
                static pattern => new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(5)));
            return regex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            _logger.LogWarning("Regex match timed out for pattern: {Pattern}", expression);
            return false;
        }
    }
}