namespace Sora.Core.Enums;

/// <summary>
///     Indicates the type of group join notification.
/// </summary>
public enum GroupJoinNotificationType
{
    /// <summary>A user directly requested to join the group.</summary>
    JoinRequest,

    /// <summary>A user was invited by a group member to join.</summary>
    InvitedJoinRequest
}