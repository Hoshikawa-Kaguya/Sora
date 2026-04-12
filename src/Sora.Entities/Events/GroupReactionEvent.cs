namespace Sora.Entities.Events;

/// <summary>Raised when a group message reaction is added or removed.</summary>
public sealed record GroupReactionEvent : BotEvent
{
    /// <summary>Face/emoji ID of the reaction.</summary>
    public string FaceId { get; init; } = "";

    /// <summary>The group.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>True if reaction added, false if removed.</summary>
    public bool IsAdd { get; init; }

    /// <summary>The message that was reacted to.</summary>
    public MessageId MessageId { get; init; }

    /// <summary>
    ///     Reaction type: <c>"face"</c> for built-in QQ faces, <c>"emoji"</c> for Unicode emoji.
    ///     Defaults to <c>"face"</c>.
    /// </summary>
    public string ReactionType { get; init; } = "face";

    /// <summary>The user who reacted.</summary>
    public UserId UserId { get; init; }
}