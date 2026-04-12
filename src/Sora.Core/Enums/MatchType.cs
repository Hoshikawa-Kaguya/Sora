namespace Sora.Core.Enums;

/// <summary>
///     Command matching strategy.
/// </summary>
public enum MatchType
{
    /// <summary>Exact full-text match.</summary>
    Full,

    /// <summary>Regular expression match.</summary>
    Regex,

    /// <summary>Keyword (contains) match.</summary>
    Keyword
}