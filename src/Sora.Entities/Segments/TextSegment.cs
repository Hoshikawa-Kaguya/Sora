namespace Sora.Entities.Segments;

/// <summary>Plain text segment.</summary>
public sealed record TextSegment() : Segment(SegmentType.Text, SegmentDirection.Both)
{
    /// <summary>The text content.</summary>
    public string Text { get; init; } = "";

    /// <summary>Implicitly converts a string to a text segment.</summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>A new <see cref="TextSegment" /> containing the text.</returns>
    public static implicit operator TextSegment(string text) => new() { Text = text };
}