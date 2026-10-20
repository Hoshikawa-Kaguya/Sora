namespace Sora.Entities.Segments;

/// <summary>Audio/voice message segment.</summary>
public sealed record AudioSegment() : Segment(SegmentType.Audio, SegmentDirection.Both)
{
    /// <summary>Resource ID (incoming only, from protocol).</summary>
    public string ResourceId { get; internal init; } = "";

    /// <summary>Temporary download URL (incoming only).</summary>
    public string Url { get; internal init; } = "";

    /// <summary>File URI for sending (outgoing: file://, http(s)://, base64://).</summary>
    public string FileUri { get; set; } = "";

    /// <summary>Duration in seconds (incoming only).</summary>
    public int Duration { get; internal init; }

    /// <inheritdoc />
    /// <remarks>Keeps <see cref="FileUri" /> when set; otherwise the result sends <see cref="Url" />.</remarks>
    public override Segment? ToOutgoing()
    {
        if (!string.IsNullOrEmpty(FileUri)) return base.ToOutgoing();
        if (string.IsNullOrEmpty(Url) || base.ToOutgoing() is not AudioSegment outgoing) return null;
        return outgoing with { FileUri = Url };
    }
}