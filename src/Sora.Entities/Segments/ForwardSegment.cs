namespace Sora.Entities.Segments;

/// <summary>Merged forward message segment.</summary>
public sealed record ForwardSegment() : Segment(SegmentType.Forward, SegmentDirection.Both)
{
    /// <summary>Nested messages (outgoing only, required for sending).</summary>
    public IReadOnlyList<ForwardedMessageNode> Messages { get; init; } = [];

    /// <summary>Forward message ID (incoming, for retrieving content via API).</summary>
    public string ForwardId { get; internal init; } = "";

    /// <summary>Forward title. Optional custom title.</summary>
    public string Title { get; init; } = "";

    /// <summary>Summary text. Optional custom summary.</summary>
    public string Summary { get; init; } = "";

    /// <summary>Preview prompt text for mobile QQ (outgoing only, LLBot extension).</summary>
    public string Prompt { get; init; } = "";

    /// <summary>Preview text lines. Optional, 1-4 lines.</summary>
    public IReadOnlyList<string> Preview { get; internal init; } = [];

    /// <inheritdoc />
    public override Segment? ToOutgoing() => Messages.Count == 0 ? null : base.ToOutgoing();
}