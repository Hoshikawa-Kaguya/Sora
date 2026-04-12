using Newtonsoft.Json;

namespace Sora.Adapter.Milky.Models.Api;

/// <summary>Input parameters for the set_group_name API.</summary>
internal sealed class SetGroupNameInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("new_group_name")]
    public string NewGroupName { get; set; } = "";
}

/// <summary>Input parameters for the set_group_avatar API.</summary>
internal sealed class SetGroupAvatarInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("image_uri")]
    public string ImageUri { get; set; } = "";
}

/// <summary>Input parameters for the set_group_member_card API.</summary>
internal sealed class SetGroupMemberCardInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("card")]
    public string Card { get; set; } = "";
}

/// <summary>Input parameters for the set_group_member_special_title API.</summary>
internal sealed class SetGroupMemberSpecialTitleInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("special_title")]
    public string SpecialTitle { get; set; } = "";
}

/// <summary>Input parameters for the set_group_member_admin API.</summary>
internal sealed class SetGroupMemberAdminInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("is_set")]
    public bool IsSet { get; set; } = true;
}

/// <summary>Input parameters for the set_group_member_mute API.</summary>
internal sealed class SetGroupMemberMuteInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("duration")]
    public long Duration { get; set; }
}

/// <summary>Input parameters for the set_group_whole_mute API.</summary>
internal sealed class SetGroupWholeMuteInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("is_mute")]
    public bool IsMute { get; set; } = true;
}

/// <summary>Input parameters for the kick_group_member API.</summary>
internal sealed class KickGroupMemberInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("reject_add_request")]
    public bool RejectAddRequest { get; set; }
}

/// <summary>Input parameters for the get_group_announcements API.</summary>
internal sealed class GetGroupAnnouncementsInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }
}

/// <summary>Output data from the get_group_announcements API.</summary>
internal sealed class GetGroupAnnouncementsOutput
{
    [JsonProperty("announcements")]
    public List<MilkyGroupAnnouncementEntity> Announcements { get; set; } = [];
}

/// <summary>Input parameters for the send_group_announcement API.</summary>
internal sealed class SendGroupAnnouncementInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; } = "";

    [JsonProperty("image_uri")]
    public string ImageUri { get; set; } = "";
}

/// <summary>Input parameters for the delete_group_announcement API.</summary>
internal sealed class DeleteGroupAnnouncementInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("announcement_id")]
    public string AnnouncementId { get; set; } = "";
}

/// <summary>Input parameters for the get_group_essence_messages API.</summary>
internal sealed class GetGroupEssenceMessagesInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("page_index")]
    public int PageIndex { get; set; }

    [JsonProperty("page_size")]
    public int PageSize { get; set; }
}

/// <summary>Output data from the get_group_essence_messages API.</summary>
internal sealed class GetGroupEssenceMessagesOutput
{
    [JsonProperty("messages")]
    public List<MilkyGroupEssenceMessage> Messages { get; set; } = [];

    [JsonProperty("is_end")]
    public bool IsEnd { get; set; }
}

/// <summary>Input parameters for the set_group_essence_message API.</summary>
internal sealed class SetGroupEssenceMessageInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("is_set")]
    public bool IsSet { get; set; } = true;
}

/// <summary>Input parameters for the quit_group API.</summary>
internal sealed class QuitGroupInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }
}

/// <summary>Input parameters for the send_group_message_reaction API.</summary>
internal sealed class SendGroupMessageReactionInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("reaction_type")]
    public string ReactionType { get; set; } = "face";

    [JsonProperty("reaction")]
    public string Reaction { get; set; } = "";

    [JsonProperty("is_add")]
    public bool IsAdd { get; set; } = true;
}

/// <summary>Input parameters for the send_group_nudge API.</summary>
internal sealed class SendGroupNudgeInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }
}

/// <summary>Input parameters for the get_group_notifications API.</summary>
internal sealed class GetGroupNotificationsInput
{
    [JsonProperty("start_notification_seq")]
    public long StartNotificationSeq { get; set; }

    [JsonProperty("limit")]
    public int Limit { get; set; } = 20;

    [JsonProperty("is_filtered")]
    public bool IsFiltered { get; set; }
}

/// <summary>Milky group notification entity.</summary>
internal sealed class MilkyGroupNotification
{
    [JsonProperty("notification_seq")]
    public long NotificationSeq { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("initiator_id")]
    public long? InitiatorId { get; set; }

    [JsonProperty("target_user_id")]
    public long? TargetUserId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("state")]
    public string? State { get; set; }

    [JsonProperty("comment")]
    public string? Comment { get; set; }

    [JsonProperty("is_filtered")]
    public bool IsFiltered { get; set; }

    [JsonProperty("is_set")]
    public bool IsSet { get; set; }
}

/// <summary>Output data from the get_group_notifications API.</summary>
internal sealed class GetGroupNotificationsOutput
{
    [JsonProperty("next_notification_seq")]
    public long NextNotificationSeq { get; set; }

    [JsonProperty("notifications")]
    public List<MilkyGroupNotification> Notifications { get; set; } = [];
}

/// <summary>Input parameters for the accept_group_request API.</summary>
internal sealed class AcceptGroupRequestInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("notification_seq")]
    public long NotificationSeq { get; set; }

    [JsonProperty("notification_type")]
    public string NotificationType { get; set; } = "";

    [JsonProperty("is_filtered")]
    public bool IsFiltered { get; set; }
}

/// <summary>Input parameters for the reject_group_request API.</summary>
internal sealed class RejectGroupRequestInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("notification_seq")]
    public long NotificationSeq { get; set; }

    [JsonProperty("notification_type")]
    public string NotificationType { get; set; } = "";

    [JsonProperty("reason")]
    public string Reason { get; set; } = "";

    [JsonProperty("is_filtered")]
    public bool IsFiltered { get; set; }
}

/// <summary>Input parameters for the accept_group_invitation API.</summary>
internal sealed class AcceptGroupInvitationInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("invitation_seq")]
    public long InvitationSeq { get; set; }
}

/// <summary>Input parameters for the reject_group_invitation API.</summary>
internal sealed class RejectGroupInvitationInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("invitation_seq")]
    public long InvitationSeq { get; set; }
}