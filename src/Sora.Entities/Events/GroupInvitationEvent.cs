namespace Sora.Entities.Events;

/// <summary>Raised when the bot is invited to join a group.</summary>
public sealed record GroupInvitationEvent : BotEvent
{
    /// <summary>The group being invited to.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>The invitation sequence number for handling the invitation.</summary>
    public long InvitationSeq { get; init; }

    /// <summary>The user who sent the invitation.</summary>
    public UserId InvitorId { get; init; }

    /// <summary>The group from which the invitation originated (may be default if unavailable).</summary>
    public GroupId SourceGroupId { get; init; }
}