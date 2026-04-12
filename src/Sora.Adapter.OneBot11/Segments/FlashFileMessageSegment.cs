namespace Sora.Adapter.OneBot11.Segments;

/// <summary>
///     OB11-specific flash file (闪传) message segment (incoming only).
///     Represents a flash file transfer embedded in a message.
///     Distinct from <see cref="Events.FlashFileDownloadedEvent" /> and related events
///     which track flash file lifecycle status changes.
/// </summary>
public sealed record FlashFileMessageSegment() : Segment(SegmentType.File, SegmentDirection.Incoming)
{
    /// <summary>Flash file set identifier.</summary>
    public string FileSetId { get; internal init; } = "";

    /// <summary>Flash file title/display name.</summary>
    public string Title { get; internal init; } = "";

    /// <summary>Scene type identifier.</summary>
    public int SceneType { get; internal init; }

    /// <inheritdoc />
    public override Segment? ToOutgoing() => null;
}