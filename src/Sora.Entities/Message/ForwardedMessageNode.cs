namespace Sora.Entities.Message;

/// <summary>A single message within a forward segment (for sending).</summary>
public sealed record ForwardedMessageNode
{
    /// <summary>Sender's user ID.</summary>
    public UserId UserId { get; init; }

    /// <summary>Sender's display name.</summary>
    public string SenderName { get; init; } = "";

    /// <summary>Message time, or null to use the protocol implementation's current time. Unspecified times are interpreted as local time.</summary>
    public DateTime? Time { get; init; }

    private readonly ReceivedMessageBody _segments = ReceivedMessageBody.Empty;

    /// <summary>Message content segments.</summary>
    /// <remarks>Each read returns a new editable copy, so edits to it are not saved back to the node.</remarks>
    public MessageBody Segments
    {
        get => _segments.ToEditable();
        init => _segments = ReceivedMessageBody.From(value);
    }
}