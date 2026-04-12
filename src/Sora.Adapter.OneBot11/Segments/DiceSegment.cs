namespace Sora.Adapter.OneBot11.Segments;

/// <summary>
///     OB11-specific dice (random 1-6) segment.
///     Must be sent alone in a message body.
/// </summary>
public sealed record DiceSegment() : Segment(SegmentType.Face, SegmentDirection.Both)
{
    /// <summary>
    ///     The dice result value ("1"-"6"). Only populated for incoming dice segments.
    /// </summary>
    public string? Result { get; init; }
}