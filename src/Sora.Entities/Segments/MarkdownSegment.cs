namespace Sora.Entities.Segments;

/// <summary>Markdown message segment (incoming only).</summary>
public sealed record MarkdownSegment() : Segment(SegmentType.Markdown, SegmentDirection.Incoming)
{
    /// <summary>Markdown content.</summary>
    public string Content { get; internal init; } = "";

    /// <inheritdoc />
    public override Segment? ToOutgoing() => null;
}