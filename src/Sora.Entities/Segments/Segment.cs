namespace Sora.Entities.Segments;

/// <summary>Base class for all message segments.</summary>
public abstract record Segment
{
    /// <summary>Initializes a new segment with the specified type and direction.</summary>
    /// <param name="type">The segment type identifier.</param>
    /// <param name="direction">The direction this segment supports.</param>
    protected Segment(SegmentType type, SegmentDirection direction)
    {
        Type      = type;
        Direction = direction;
    }

    /// <summary>The segment type identifier.</summary>
    public SegmentType Type { get; internal set; }

    /// <summary>The direction this segment supports (incoming, outgoing, or both).</summary>
    public SegmentDirection Direction { get; internal set; }

    /// <summary>
    ///     Converts this segment to an outgoing-compatible form.
    ///     Return null when conversion is impossible (incoming-only segment).
    /// </summary>
    /// <returns>An outgoing-compatible segment, or null if conversion is not possible.</returns>
    public virtual Segment? ToOutgoing()
    {
        Direction = SegmentDirection.Outgoing;
        return this;
    }

    /// <summary>Concatenates two segments into a <see cref="MessageBody" />.</summary>
    /// <param name="left">The first segment.</param>
    /// <param name="right">The second segment.</param>
    /// <returns>A new <see cref="MessageBody" /> containing both segments.</returns>
    public static MessageBody operator +(Segment left, Segment right) => new([left, right]);

    /// <summary>Concatenates a segment and a string into a <see cref="MessageBody" />.</summary>
    /// <param name="left">The segment.</param>
    /// <param name="right">The text string to append.</param>
    /// <returns>A new <see cref="MessageBody" /> containing the segment and text.</returns>
    public static MessageBody operator +(Segment left, string right) => new([left, new TextSegment { Text = right }]);

    /// <summary>Concatenates a string and a segment into a <see cref="MessageBody" />.</summary>
    /// <param name="left">The text string to prepend.</param>
    /// <param name="right">The segment.</param>
    /// <returns>A new <see cref="MessageBody" /> containing the text and segment.</returns>
    public static MessageBody operator +(string left, Segment right) => new([new TextSegment { Text = left }, right]);
}