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
    public MessageBody Body { get; init; } = [];

    /// <summary>Sender's avatar URL (available in forwarded messages). LLBot extension.</summary>
    public string AvatarUrl { get; init; } = "";

    /// <summary>When the message was sent.</summary>
    public DateTime Time { get; init; }
}