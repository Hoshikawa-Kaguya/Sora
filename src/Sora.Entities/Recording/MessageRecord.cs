namespace Sora.Entities.Recording;

/// <summary>
///     Read-only snapshot of one chat message in one direction, delivered to <see cref="IMessageRecorder" />.
///     Inbound and outbound messages share this type; it carries no API or business capability.
/// </summary>
/// <remarks>
///     Segments are copies independent from the originating event or caller message, so later
///     setter changes on the originals do not affect the record.
///     Forward segments are represented by <see cref="ForwardRecordSegment" />.
/// </remarks>
public sealed record MessageRecord
{
#region Properties

    /// <summary>Gets whether the message was received or sent by the bot.</summary>
    public RecordDirection Direction { get; internal init; }

    /// <summary>Gets the protocol name of the adapter that produced the record.</summary>
    public string ProtocolName { get; internal init; } = "";

    /// <summary>Gets the connection on which the message was received or sent.</summary>
    public Guid ConnectionId { get; internal init; }

    /// <summary>Gets the bot account user ID.</summary>
    public UserId SelfId { get; internal init; }

    /// <summary>
    ///     Gets the protocol time of the message. Outbound records fall back to the local time at which the
    ///     successful response was received when the protocol reports no time; nested forward nodes without
    ///     a time use the default value.
    /// </summary>
    public DateTime Time { get; internal init; }

    /// <summary>Gets the protocol message ID.</summary>
    public MessageId MessageId { get; internal init; }

    /// <summary>Gets the message source type.</summary>
    public MessageSourceType SourceType { get; internal init; }

    /// <summary>Gets the group ID for group messages.</summary>
    public GroupId GroupId { get; internal init; }

    /// <summary>Gets the other party of a friend or temporary-session message: the sender when inbound, the target when outbound.</summary>
    public UserId PeerUserId { get; internal init; }

    /// <summary>Gets the sender user ID; equals <see cref="SelfId" /> for outbound records.</summary>
    public UserId SenderId { get; internal init; }

    /// <summary>Gets the sender display name when the source provides one.</summary>
    public string SenderName { get; internal init; } = "";

    /// <summary>Gets the recorded message segments.</summary>
    public IReadOnlyList<Segment> Segments { get; internal init; } = [];

    /// <summary>Gets the sender information for inbound records; null for outbound records.</summary>
    public UserInfo? Sender { get; internal init; }

    /// <summary>Gets the group information for inbound records; null for outbound records.</summary>
    public GroupInfo? Group { get; internal init; }

    /// <summary>Gets the group member information for inbound records; null for outbound records.</summary>
    public GroupMemberInfo? Member { get; internal init; }

#endregion

#region Factories

    /// <summary>Creates an inbound record describing the same message as a received message event.</summary>
    /// <param name="protocolName">The protocol name of the producing adapter.</param>
    /// <param name="e">The converted message event.</param>
    /// <returns>A record whose segments are independent copies of the event segments.</returns>
    internal static MessageRecord Inbound(string protocolName, MessageReceivedEvent e) => new()
    {
        Direction    = RecordDirection.Inbound,
        ProtocolName = protocolName,
        ConnectionId = e.ConnectionId,
        SelfId       = e.SelfId,
        Time         = e.Message.Time,
        MessageId    = e.Message.MessageId,
        SourceType   = e.Message.SourceType,
        GroupId      = e.Message.GroupId,
        PeerUserId   = e.Message.SourceType == MessageSourceType.Group ? default : e.Message.SenderId,
        SenderId     = e.Message.SenderId,
        SenderName   = e.Message.SenderName,
        Segments     = CopySegments(e.Message.BodySnapshot.Segments, RecordDirection.Inbound),
        Sender       = e.Sender,
        Group        = e.Group,
        Member       = e.Member
    };

    /// <summary>
    ///     Creates a pending outbound record before the send call, snapshotting the submitted segments so that
    ///     caller changes while awaiting the response do not affect the record. Complete it with
    ///     <see cref="Accept" /> once the protocol accepted the message.
    /// </summary>
    /// <param name="protocolName">The protocol name of the producing adapter.</param>
    /// <param name="connectionId">The connection used to send the message.</param>
    /// <param name="selfId">The bot account user ID.</param>
    /// <param name="sourceType">The target conversation type.</param>
    /// <param name="peerUserId">The target user for friend or temporary-session messages; default for groups.</param>
    /// <param name="groupId">The target group for group messages; default otherwise.</param>
    /// <param name="submittedSegments">The segments actually submitted to the protocol.</param>
    /// <returns>A pending record whose segments are independent copies of the submitted segments.</returns>
    internal static MessageRecord Outbound(
        string               protocolName,
        Guid                 connectionId,
        UserId               selfId,
        MessageSourceType    sourceType,
        UserId               peerUserId,
        GroupId              groupId,
        IEnumerable<Segment> submittedSegments) => new()
    {
        Direction    = RecordDirection.Outbound,
        ProtocolName = protocolName,
        ConnectionId = connectionId,
        SelfId       = selfId,
        SourceType   = sourceType,
        GroupId      = groupId,
        PeerUserId   = peerUserId,
        SenderId     = selfId,
        Segments     = CopySegments(submittedSegments, RecordDirection.Outbound)
    };

    /// <summary>Completes a pending outbound record with the protocol response.</summary>
    /// <param name="result">The successful send result.</param>
    /// <returns>The record carrying the protocol message ID and time, or the local time when none was reported.</returns>
    internal MessageRecord Accept(SendMessageResult result) =>
        this with { MessageId = result.MessageId, Time = result.Time ?? DateTime.Now };

#endregion

#region Segment Copy

    /// <summary>Copies segments for recording, replacing forward segments with read-only forward records.</summary>
    /// <param name="segments">The source segments.</param>
    /// <param name="direction">The direction applied to nested forward records.</param>
    /// <returns>Independent segment copies.</returns>
    internal static IReadOnlyList<Segment> CopySegments(IEnumerable<Segment> segments, RecordDirection direction) =>
        [.. segments.Select(s => CopySegment(s, direction))];

    private static Segment CopySegment(Segment segment, RecordDirection direction) =>
        segment switch
        {
            // Received forwards come from the protocol; sent forwards are always built by the user.
            ForwardSegment forward => new ForwardRecordSegment
            {
                Direction = direction == RecordDirection.Outbound
                    ? SegmentDirection.Outgoing
                    : SegmentDirection.Incoming,
                Messages  = [.. forward.Messages.Select(n => CopyForwardNode(n, direction))],
                ForwardId = forward.ForwardId,
                Title     = forward.Title,
                Summary   = forward.Summary,
                Prompt    = forward.Prompt,
                Preview   = [.. forward.Preview]
            },
            _ => segment with { }
        };

    private static MessageRecord CopyForwardNode(ForwardedMessageNode node, RecordDirection direction) => new()
    {
        Direction  = direction,
        SenderId   = node.UserId,
        SenderName = node.SenderName,
        Time       = node.Time ?? default,
        Segments   = CopySegments(node.Segments, direction)
    };

#endregion
}