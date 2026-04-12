namespace Sora.Entities.Segments;

/// <summary>File attachment segment (incoming only).</summary>
public sealed record FileSegment() : Segment(SegmentType.File, SegmentDirection.Incoming)
{
    /// <summary>File identifier.</summary>
    public string FileId { get; internal init; } = "";

    /// <summary>File name.</summary>
    public string FileName { get; internal init; } = "";

    /// <summary>File hash (incoming only, Milky: TriSHA1, optional — may be empty for group files).</summary>
    public string FileHash { get; internal init; } = "";

    /// <summary>File size in bytes.</summary>
    public long FileSize { get; internal init; }

    /// <inheritdoc />
    public override Segment? ToOutgoing() => null;
}