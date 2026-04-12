namespace Sora.Adapter.OneBot11.Segments;

/// <summary>
///     OB11-specific markdown rich text segment.
///     Contains markdown-formatted content for display in QQ clients that support it.
///     Uses <see cref="SegmentType.Text" /> as the base type since markdown is a form of text content.
/// </summary>
public sealed record MarkdownSegment() : Segment(SegmentType.Text, SegmentDirection.Both)
{
    /// <summary>Markdown content text.</summary>
    public string Content { get; init; } = "";
}