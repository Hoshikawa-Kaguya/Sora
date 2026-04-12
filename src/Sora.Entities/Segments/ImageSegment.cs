namespace Sora.Entities.Segments;

/// <summary>Image segment.</summary>
public sealed record ImageSegment() : Segment(SegmentType.Image, SegmentDirection.Both)
{
    /// <summary>Image sub-type (normal or sticker).</summary>
    public ImageSubType SubType { get; init; } = ImageSubType.Normal;

    /// <summary>Resource ID (incoming only, from protocol).</summary>
    public string ResourceId { get; internal init; } = "";

    /// <summary>Temporary download URL (incoming only).</summary>
    public string Url { get; internal init; } = "";

    /// <summary>File URI for sending (outgoing: file://, http(s)://, base64://).</summary>
    public string FileUri { get; set; } = "";

    /// <summary>Image width in pixels (incoming only).</summary>
    public int Width { get; internal init; }

    /// <summary>Image height in pixels (incoming only).</summary>
    public int Height { get; internal init; }

    /// <summary>Preview text. Optional.</summary>
    public string Summary { get; init; } = "";

    /// <inheritdoc />
    public override Segment? ToOutgoing()
    {
        if (FileUri.Length > 0) return base.ToOutgoing();
        if (Url.Length <= 0) return null;
        FileUri = Url;
        return base.ToOutgoing();
    }
}