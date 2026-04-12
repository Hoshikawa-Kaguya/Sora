namespace Sora.Entities.Segments;

/// <summary>XML rich message segment (incoming only).</summary>
public sealed record XmlSegment() : Segment(SegmentType.Xml, SegmentDirection.Incoming)
{
    /// <summary>Service ID.</summary>
    public int ServiceId { get; internal init; }

    /// <summary>XML payload string.</summary>
    public string XmlPayload { get; internal init; } = "";

    /// <inheritdoc />
    public override Segment? ToOutgoing() => null;
}