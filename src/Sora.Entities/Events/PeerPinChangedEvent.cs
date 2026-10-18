namespace Sora.Entities.Events;

/// <summary>Raised when a conversation's pin (top) state is changed. Milky-specific.</summary>
public sealed record PeerPinChangedEvent : BotEvent, ISourcedEvent
{
    /// <summary>Whether the peer is now pinned (topped).</summary>
    public bool IsPinned { get; init; }

    /// <summary>The peer ID (user ID for friends and temporary sessions, group ID for groups).</summary>
    public long PeerId { get; init; }

    /// <summary>The message source of the changed conversation.</summary>
    public MessageSourceType SourceType { get; init; }
}
