namespace Sora.Entities.Message;

/// <summary>A single message within a forward segment (for sending).</summary>
public sealed record ForwardedMessageNode
{
    /// <summary>Sender's user ID.</summary>
    public UserId UserId { get; init; }

    /// <summary>Sender's display name.</summary>
    public string SenderName { get; init; } = "";

    /// <summary>Message content segments.</summary>
    public MessageBody Segments { get; init; } = [];
}