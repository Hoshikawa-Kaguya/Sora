namespace Sora.Entities.Segments;

/// <summary>QQ face emoji segment.</summary>
public sealed record FaceSegment() : Segment(SegmentType.Face, SegmentDirection.Both)
{
    /// <summary>Face emoji identifier.</summary>
    public string FaceId { get; init; } = "";

    /// <summary>Whether this is a super/large face.</summary>
    public bool IsLarge { get; init; } = false;
}