namespace Sora.Entities.Events;

/// <summary>Raised when a message is received.</summary>
public sealed record MessageReceivedEvent : BotEvent, ISourcedEvent
{
    /// <summary>The message source, taken from <see cref="MessageContext.SourceType" /> of <see cref="Message" />.</summary>
    MessageSourceType ISourcedEvent.SourceType => Message.SourceType;

    /// <summary>Group information (default for private messages).</summary>
    public GroupInfo Group { get; init; } = new();

    /// <summary>Group member information (default for private messages).</summary>
    public GroupMemberInfo Member { get; init; } = new();

    /// <summary>The received message with full context.</summary>
    public MessageContext Message { get; init; } = new();

    /// <summary>Sender information.</summary>
    public UserInfo Sender { get; init; } = new();
}