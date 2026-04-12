namespace Sora.Entities.Events;

/// <summary>Raised when a conversation's pin (top) state is changed. Milky-specific.</summary>
public sealed record PeerPinChangedEvent : BotEvent
{
    /// <summary>Whether the peer is now pinned (topped).</summary>
    public bool IsPinned { get; init; }

    /// <summary>The message scene: <c>"friend"</c> or <c>"group"</c>.</summary>
    public string MessageScene { get; init; } = "";

    /// <summary>The peer ID (user ID for friends, group ID for groups).</summary>
    public long PeerId { get; init; }
}