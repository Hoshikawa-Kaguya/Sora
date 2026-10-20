namespace Sora.Entities.Message;

/// <summary>
///     Represents a received or retrieved message with full context.
/// </summary>
public sealed record MessageContext
{
    /// <summary>Message unique identifier.</summary>
    public MessageId MessageId { get; init; }

    /// <summary>Group ID (default for private messages).</summary>
    public GroupId GroupId { get; init; }

    /// <summary>Sender's user ID.</summary>
    public UserId SenderId { get; init; }

    /// <summary>Sender's display name (available in forwarded messages).</summary>
    public string SenderName { get; init; } = "";

    /// <summary>Message source type (private, group, temp).</summary>
    public MessageSourceType SourceType { get; init; }

    /// <summary>Message content segments.</summary>
    /// <remarks>Each read returns a new editable copy; read it once into a local variable for repeated use.</remarks>
    public MessageBody Body
    {
        get => BodySnapshot.ToEditable();
        init => BodySnapshot = ReceivedMessageBody.From(value);
    }

    /// <summary>Gets the unchanged message content for framework-internal reads without copying.</summary>
    internal ReceivedMessageBody BodySnapshot { get; private set; } = ReceivedMessageBody.Empty;

    /// <summary>Sender's avatar URL (available in forwarded messages). LLBot extension.</summary>
    public string AvatarUrl { get; init; } = "";

    /// <summary>When the message was sent.</summary>
    public DateTime Time { get; init; }
}