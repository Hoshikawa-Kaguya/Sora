namespace Sora.Command.Matching;

/// <summary>
///     Matches when the input exactly equals the expression.
/// </summary>
public sealed class FullMatcher : ICommandMatcher
{
    /// <inheritdoc />
    public MatchType MatchType => MatchType.Full;

    /// <inheritdoc />
    public bool IsMatch(string input, string expression) => string.Equals(input, expression, StringComparison.Ordinal);
}