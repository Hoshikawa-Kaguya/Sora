namespace Sora.Core.Enums;

/// <summary>Direction a segment can be used in.</summary>
public enum SegmentDirection
{
    /// <summary>Can only be received, not sent.</summary>
    Incoming,

    /// <summary>Can only be sent, not received.</summary>
    Outgoing,

    /// <summary>Can be both received and sent.</summary>
    Both
}