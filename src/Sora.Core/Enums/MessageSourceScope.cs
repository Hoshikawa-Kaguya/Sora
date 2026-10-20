namespace Sora.Core.Enums;

/// <summary>
///     A declared set of message sources accepted by a command or an event filter.
///     Values can be combined (for example <c>Friend | Temp</c>); a scope never describes the source of a received event.
/// </summary>
[Flags]
public enum MessageSourceScope
{
    /// <summary>Private (direct) messages.</summary>
    Friend = 1,

    /// <summary>Group messages.</summary>
    Group = 2,

    /// <summary>Temporary session messages (Milky-specific).</summary>
    Temp = 4,

    /// <summary>All message sources; no source restriction is applied.</summary>
    Any = Friend | Group | Temp
}