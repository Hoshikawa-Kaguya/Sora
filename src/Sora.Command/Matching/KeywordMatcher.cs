namespace Sora.Command.Matching;

/// <summary>
///     Matches when the input contains the expression as a substring.
/// </summary>
public sealed class KeywordMatcher : ICommandMatcher
{
    /// <inheritdoc />
    public MatchType MatchType => MatchType.Keyword;

    /// <inheritdoc />
    public bool IsMatch(string input, string expression) => input.Contains(expression, StringComparison.Ordinal);
}