namespace Sora.Entities.Info;

/// <summary>Group essence message information.</summary>
public sealed record GroupEssenceMessageInfo
{
    /// <summary>Group ID.</summary>
    public GroupId GroupId { get; internal init; }

    /// <summary>Message sequence number.</summary>
    public MessageId MessageId { get; internal init; }

    /// <summary>Message sender's user ID.</summary>
    public UserId SenderId { get; internal init; }

    /// <summary>Sender's display name.</summary>
    public string SenderName { get; internal init; } = "";

    /// <summary>Operator who set the essence.</summary>
    public UserId OperatorId { get; internal init; }

    /// <summary>Operator's display name.</summary>
    public string OperatorName { get; internal init; } = "";

    private readonly ReceivedMessageBody _body = ReceivedMessageBody.Empty;

    /// <summary>Message content segments.</summary>
    /// <remarks>Each read returns a new editable copy.</remarks>
    public MessageBody Body
    {
        get => _body.ToEditable();
        internal init => _body = ReceivedMessageBody.From(value);
    }

    /// <summary>Message send timestamp.</summary>
    public DateTime MessageTime { get; internal init; }

    /// <summary>When the message was set as essence.</summary>
    public DateTime OperationTime { get; internal init; }
}