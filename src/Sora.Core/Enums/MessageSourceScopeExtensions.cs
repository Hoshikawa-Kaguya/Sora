namespace Sora.Core.Enums;

/// <summary>
///     Helpers for evaluating a <see cref="MessageSourceScope" /> against a concrete <see cref="MessageSourceType" />.
/// </summary>
public static class MessageSourceScopeExtensions
{
    /// <summary>
    ///     Returns whether <paramref name="scope" /> contains <paramref name="source" />.
    ///     Undefined source values are never contained.
    /// </summary>
    /// <param name="scope">The declared source scope.</param>
    /// <param name="source">The concrete source of a received event.</param>
    /// <returns><c>true</c> if the source belongs to the scope; otherwise <c>false</c>.</returns>
    public static bool Contains(this MessageSourceScope scope, MessageSourceType source)
    {
        MessageSourceScope flag = source switch
                                  {
                                      MessageSourceType.Friend => MessageSourceScope.Friend,
                                      MessageSourceType.Group  => MessageSourceScope.Group,
                                      MessageSourceType.Temp   => MessageSourceScope.Temp,
                                      _                        => 0
                                  };
        return (scope & flag) != 0;
    }
}
