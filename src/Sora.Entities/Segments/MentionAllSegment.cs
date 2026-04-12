namespace Sora.Entities.Segments;

/// <summary>Mention all (@all) segment.</summary>
public sealed record MentionAllSegment() : Segment(SegmentType.MentionAll, SegmentDirection.Both);