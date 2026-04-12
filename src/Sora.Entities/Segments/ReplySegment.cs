namespace Sora.Entities.Segments;

/// <summary>Reply to a message segment.</summary>
public sealed record ReplySegment() : Segment(SegmentType.Reply, SegmentDirection.Both)
{
    /// <summary>ID of the message being replied to.</summary>
    public MessageId TargetId { get; init; }

    /// <summary>Sender of the quoted message (incoming only, from protocol).</summary>
    public UserId SenderId { get; internal init; }

    /// <summary>Sender name of the quoted message (incoming only, from protocol). Only available in forwarded messages.</summary>
    public string? SenderName { get; internal init; }

    /// <summary>Content segments of the quoted message (incoming only, from protocol).</summary>
    public MessageBody? Content { get; internal init; }

    /// <summary>Unix timestamp (seconds) of the quoted message (incoming only, from protocol).</summary>
    public long Time { get; internal init; }
}