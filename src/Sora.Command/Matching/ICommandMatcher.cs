namespace Sora.Command.Matching;

/// <summary>
///     Interface for command matching strategies.
/// </summary>
public interface ICommandMatcher
{
    /// <summary>Gets the match type this matcher handles.</summary>
    MatchType MatchType { get; }

    /// <summary>
    ///     Tests whether the input text matches the given expression.
    /// </summary>
    /// <param name="input">The message text to match against.</param>
    /// <param name="expression">The match expression.</param>
    /// <returns>True if the input matches; otherwise false.</returns>
    bool IsMatch(string input, string expression);
}