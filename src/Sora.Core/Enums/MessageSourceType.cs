namespace Sora.Core.Enums;

/// <summary>
///     Indicates the source/scene of a message.
/// </summary>
public enum MessageSourceType
{
    /// <summary>Private (direct) message.</summary>
    Friend = 1,

    /// <summary>Group message.</summary>
    Group = 2,

    /// <summary>Temporary session message (Milky-specific).</summary>
    Temp = 3
}