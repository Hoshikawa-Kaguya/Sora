namespace Sora.Entities.Segments;

/// <summary>
///     Light app / mini program segment.
///     (outgoing segment for LLBot extension).
/// </summary>
public sealed record LightAppSegment() : Segment(SegmentType.LightApp, SegmentDirection.Both)
{
    /// <summary> App name (incoming only, for display). </summary>
    public string AppName { get; internal init; } = "";

    /// <summary> JSON payload string. </summary>
    public string JsonPayload { get; init; } = "";
}