namespace Sora.Entities.Segments;

/// <summary>Mention (@someone) segment.</summary>
public sealed record MentionSegment() : Segment(SegmentType.Mention, SegmentDirection.Both)
{
    /// <summary>The user being mentioned.</summary>
    public UserId Target { get; init; }

    /// <summary>Display name of the mentioned user (incoming only, from protocol).</summary>
    public string Name { get; internal init; } = "";
}