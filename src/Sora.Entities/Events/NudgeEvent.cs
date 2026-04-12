namespace Sora.Entities.Events;

/// <summary>Raised when a nudge (poke) is received.</summary>
public sealed record NudgeEvent : BotEvent
{
    /// <summary>Display action image URL (replaces action text when present).</summary>
    public string ActionImageUrl { get; init; } = "";

    /// <summary>Display action text.</summary>
    public string ActionText { get; init; } = "";

    /// <summary>Group ID (0 for private).</summary>
    public GroupId GroupId { get; init; }

    /// <summary>The user who was nudged.</summary>
    public UserId ReceiverId { get; init; }

    /// <summary>The user who sent the nudge.</summary>
    public UserId SenderId { get; init; }

    /// <summary>Whether this was a private or group nudge.</summary>
    public MessageSourceType SourceType { get; init; }

    /// <summary>Display suffix text.</summary>
    public string SuffixText { get; init; } = "";
}