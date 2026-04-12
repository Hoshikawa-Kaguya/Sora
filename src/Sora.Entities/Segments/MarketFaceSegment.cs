namespace Sora.Entities.Segments;

/// <summary>Market face (QQ store emoji) segment (incoming only).</summary>
public sealed record MarketFaceSegment() : Segment(SegmentType.MarketFace, SegmentDirection.Incoming)
{
    /// <summary>Emoji ID.</summary>
    public string EmojiId { get; internal init; } = "";

    /// <summary>Emoji package ID.</summary>
    public long EmojiPackageId { get; internal init; }

    /// <summary>Emoji key.</summary>
    public string Key { get; internal init; } = "";

    /// <summary>Emoji URL.</summary>
    public string Url { get; internal init; } = "";

    /// <summary>Preview text.</summary>
    public string Summary { get; internal init; } = "";

    /// <inheritdoc />
    public override Segment? ToOutgoing() => null;
}