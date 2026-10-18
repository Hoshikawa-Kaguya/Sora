namespace Sora.Entities.Recording;

/// <summary>
///     Read-only forward segment used only inside <see cref="MessageRecord" />.
///     Nested messages are themselves records, so the content contains no <see cref="MessageBody" />.
///     <see cref="Segment.Direction" /> is <see cref="SegmentDirection.Incoming" /> in inbound records and
///     <see cref="SegmentDirection.Outgoing" /> in outbound records, where the forward was built by the user.
/// </summary>
public sealed record ForwardRecordSegment() : Segment(SegmentType.Forward, SegmentDirection.Incoming)
{
    /// <summary>Gets the nested forwarded messages; empty when the protocol only provided a forward ID.</summary>
    public IReadOnlyList<MessageRecord> Messages { get; internal init; } = [];

    /// <summary>Gets the forward message ID when received from the protocol.</summary>
    public string ForwardId { get; internal init; } = "";

    /// <summary>Gets the forward title.</summary>
    public string Title { get; internal init; } = "";

    /// <summary>Gets the summary text.</summary>
    public string Summary { get; internal init; } = "";

    /// <summary>Gets the mobile preview prompt text.</summary>
    public string Prompt { get; internal init; } = "";

    /// <summary>Gets the preview text lines.</summary>
    public IReadOnlyList<string> Preview { get; internal init; } = [];

    /// <summary>Record segments cannot be sent.</summary>
    /// <returns>Always null.</returns>
    public override Segment? ToOutgoing() => null;
}