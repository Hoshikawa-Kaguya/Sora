namespace Sora.Adapter.OneBot11.Segments;

/// <summary>
///     OB11-specific Rock-Paper-Scissors segment.
///     Must be sent alone in a message body.
/// </summary>
public sealed record RpsSegment() : Segment(SegmentType.Face, SegmentDirection.Both)
{
    /// <summary>
    ///     The RPS result value. "1"=Rock, "2"=Scissors, "3"=Paper.
    ///     Only populated for incoming RPS segments.
    /// </summary>
    public string? Result { get; init; }
}