namespace Sora.Entities.Info;

/// <summary>Result of a history messages query.</summary>
public sealed record HistoryMessagesResult
{
    /// <summary>Next page start sequence (null if no more).</summary>
    public MessageId? NextMessageSeq { get; internal init; }

    /// <summary>Retrieved messages (message_seq ascending).</summary>
    public IReadOnlyList<MessageContext> Messages { get; internal init; } = [];
}