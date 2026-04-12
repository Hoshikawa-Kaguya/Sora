using Newtonsoft.Json;

namespace Sora.Adapter.Milky.Models;

/// <summary>Event data for bot_offline events.</summary>
internal sealed class MilkyBotOfflineData
{
    [JsonProperty("reason")]
    public string? Reason { get; set; }
}

/// <summary>Event data for message_recall events.</summary>
internal sealed class MilkyMessageRecallData
{
    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("peer_id")]
    public long PeerId { get; set; }

    [JsonProperty("sender_id")]
    public long SenderId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("message_scene")]
    public string? MessageScene { get; set; }

    [JsonProperty("display_suffix")]
    public string? DisplaySuffix { get; set; }
}

/// <summary>Event data for friend_request events.</summary>
internal sealed class MilkyFriendRequestData
{
    [JsonProperty("initiator_id")]
    public long InitiatorId { get; set; }

    [JsonProperty("initiator_uid")]
    public string? InitiatorUid { get; set; }

    [JsonProperty("comment")]
    public string? Comment { get; set; }

    [JsonProperty("via")]
    public string? Via { get; set; }
}

/// <summary>Event data for group_join_request events.</summary>
internal sealed class MilkyGroupJoinRequestData
{
    [JsonProperty("notification_seq")]
    public long NotificationSeq { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("initiator_id")]
    public long InitiatorId { get; set; }

    [JsonProperty("comment")]
    public string? Comment { get; set; }

    [JsonProperty("is_filtered")]
    public bool IsFiltered { get; set; }
}

/// <summary>Event data for group_invited_join_request events.</summary>
internal sealed class MilkyGroupInvitedJoinRequestData
{
    [JsonProperty("notification_seq")]
    public long NotificationSeq { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("initiator_id")]
    public long InitiatorId { get; set; }

    [JsonProperty("target_user_id")]
    public long TargetUserId { get; set; }
}

/// <summary>Event data for group_invitation events.</summary>
internal sealed class MilkyGroupInvitationData
{
    [JsonProperty("invitation_seq")]
    public long InvitationSeq { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("source_group_id")]
    public long SourceGroupId { get; set; }

    [JsonProperty("initiator_id")]
    public long InitiatorId { get; set; }
}

/// <summary>Event data for friend_nudge events.</summary>
internal sealed class MilkyFriendNudgeData
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("display_action")]
    public string? DisplayAction { get; set; }

    [JsonProperty("display_suffix")]
    public string? DisplaySuffix { get; set; }

    [JsonProperty("display_action_img_url")]
    public string? DisplayActionImgUrl { get; set; }

    [JsonProperty("is_self_receive")]
    public bool IsSelfReceive { get; set; }

    [JsonProperty("is_self_send")]
    public bool IsSelfSend { get; set; }
}

/// <summary>Event data for friend_file_upload events.</summary>
internal sealed class MilkyFriendFileUploadData
{
    [JsonProperty("file_id")]
    public string? FileId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("file_name")]
    public string? FileName { get; set; }

    [JsonProperty("file_hash")]
    public string? FileHash { get; set; }

    [JsonProperty("file_size")]
    public long FileSize { get; set; }

    [JsonProperty("is_self")]
    public bool IsSelf { get; set; }
}

/// <summary>Event data for group_admin_change events.</summary>
internal sealed class MilkyGroupAdminChangeData
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("is_set")]
    public bool IsSet { get; set; }
}

/// <summary>Event data for group_essence_message_change events.</summary>
internal sealed class MilkyGroupEssenceMessageChangeData
{
    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("is_set")]
    public bool IsSet { get; set; }
}

/// <summary>Event data for group_member_increase events.</summary>
internal sealed class MilkyGroupMemberIncreaseData
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("operator_id")]
    public long? OperatorId { get; set; }

    [JsonProperty("invitor_id")]
    public long? InvitorId { get; set; }
}

/// <summary>Event data for group_member_decrease events.</summary>
internal sealed class MilkyGroupMemberDecreaseData
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("operator_id")]
    public long? OperatorId { get; set; }
}

/// <summary>Event data for group_name_change events.</summary>
internal sealed class MilkyGroupNameChangeData
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("new_group_name")]
    public string? NewGroupName { get; set; }
}

/// <summary>Event data for group_message_reaction events.</summary>
internal sealed class MilkyGroupMessageReactionData
{
    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("reaction_type")]
    public string? ReactionType { get; set; }

    [JsonProperty("face_id")]
    public string? FaceId { get; set; }

    [JsonProperty("is_add")]
    public bool IsAdd { get; set; }
}

/// <summary>Event data for group_mute events.</summary>
internal sealed class MilkyGroupMuteData
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("duration")]
    public int Duration { get; set; }
}

/// <summary>Event data for group_whole_mute events.</summary>
internal sealed class MilkyGroupWholeMuteData
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("is_mute")]
    public bool IsMute { get; set; }
}

/// <summary>Event data for group_nudge events.</summary>
internal sealed class MilkyGroupNudgeData
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("sender_id")]
    public long SenderId { get; set; }

    [JsonProperty("receiver_id")]
    public long ReceiverId { get; set; }

    [JsonProperty("display_action")]
    public string? DisplayAction { get; set; }

    [JsonProperty("display_suffix")]
    public string? DisplaySuffix { get; set; }

    [JsonProperty("display_action_img_url")]
    public string? DisplayActionImgUrl { get; set; }
}

/// <summary>Event data for group_file_upload events.</summary>
internal sealed class MilkyGroupFileUploadData
{
    [JsonProperty("file_id")]
    public string? FileId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("file_name")]
    public string? FileName { get; set; }

    [JsonProperty("file_size")]
    public long FileSize { get; set; }
}

/// <summary>Event data for peer_pin_change events.</summary>
internal sealed class MilkyPeerPinChangeData
{
    [JsonProperty("peer_id")]
    public long PeerId { get; set; }

    [JsonProperty("message_scene")]
    public string? MessageScene { get; set; }

    [JsonProperty("is_pinned")]
    public bool IsPinned { get; set; }
}