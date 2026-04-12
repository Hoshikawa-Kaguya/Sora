namespace Sora.Entities.Events;

/// <summary>Raised when a message is received.</summary>
public sealed record MessageReceivedEvent : BotEvent
{
    /// <summary>Group information (default for private messages).</summary>
    public GroupInfo Group { get; init; } = new();

    /// <summary>Group member information (default for private messages).</summary>
    public GroupMemberInfo Member { get; init; } = new();

    /// <summary>The received message with full context.</summary>
    public MessageContext Message { get; init; } = new();

    /// <summary>Sender information.</summary>
    public UserInfo Sender { get; init; } = new();
}