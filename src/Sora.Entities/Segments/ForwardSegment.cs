namespace Sora.Entities.Segments;

/// <summary>Merged forward message segment.</summary>
public sealed record ForwardSegment() : Segment(SegmentType.Forward, SegmentDirection.Both)
{
    private readonly IReadOnlyList<ForwardedMessageNode> _messages = [];

    /// <summary>Nested messages (outgoing only, required for sending).</summary>
    /// <remarks>The assigned list is copied into a read-only collection, so later changes to it are not observed.</remarks>
    public IReadOnlyList<ForwardedMessageNode> Messages
    {
        get => _messages;
        init => _messages = Array.AsReadOnly<ForwardedMessageNode>([.. value]);
    }

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
    /// <remarks>Converts the segments of every nested message, including nested forwards.</remarks>
    public override Segment? ToOutgoing()
    {
        if (Messages.Count == 0 || base.ToOutgoing() is not ForwardSegment outgoing) return null;
        return outgoing with { Messages = [.. Messages.Select(n => n with { Segments = n.Segments.ToOutgoing() })] };
    }
}