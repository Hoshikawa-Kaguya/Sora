using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Parameters for the mark_msg_as_read action.</summary>
internal sealed class MarkMsgAsReadParams
{
    [JsonProperty("message_id")]
    public int MessageId { get; set; }
}

/// <summary>Parameters for the set_qq_avatar action.</summary>
internal sealed class SetQQAvatarParams
{
    [JsonProperty("file")]
    public string File { get; set; } = "";
}

/// <summary>Parameters for the set_qq_profile action.</summary>
internal sealed class SetQQProfileParams
{
    [JsonProperty("nickname")]
    public string? Nickname { get; set; }

    [JsonProperty("personal_note")]
    public string? PersonalNote { get; set; }
}

/// <summary>Parameters for the set_msg_emoji_like action.</summary>
internal sealed class SetMsgEmojiLikeParams
{
    [JsonProperty("message_id")]
    public int MessageId { get; set; }

    [JsonProperty("emoji_id")]
    public string EmojiId { get; set; } = "";

    [JsonProperty("set")]
    public bool Set { get; set; } = true;
}

/// <summary>Parameters for the friend_poke action.</summary>
internal sealed class FriendPokeParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("target_id", NullValueHandling = NullValueHandling.Ignore)]
    public long? TargetId { get; set; }
}

/// <summary>Parameters for the group_poke action.</summary>
internal sealed class GroupPokeParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }
}

/// <summary>Parameters for the get_doubt_friends_add_request action.</summary>
internal sealed class GetDoubtFriendsAddRequestParams
{
    [JsonProperty("count")]
    public int Count { get; set; } = 50;
}

/// <summary>Response item from the get_doubt_friends_add_request action.</summary>
internal sealed class DoubtFriendRequestItem
{
    [JsonProperty("uin")]
    public string? Uin { get; set; }

    [JsonProperty("nick")]
    public string? Nick { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("msg")]
    public string? Msg { get; set; }

    [JsonProperty("reason")]
    public string? Reason { get; set; }

    [JsonProperty("flag")]
    public string? Flag { get; set; }

    [JsonProperty("source")]
    public string? Source { get; set; }

    [JsonProperty("group_code")]
    public string? GroupCode { get; set; }

    [JsonProperty("time")]
    public string? Time { get; set; }
}

/// <summary>Parameters for the set_group_portrait action.</summary>
internal sealed class SetGroupPortraitParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("file")]
    public string File { get; set; } = "";
}

/// <summary>Parameters for the _get_group_notice action.</summary>
internal sealed class GetGroupNoticeParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }
}

/// <summary>Response item from the _get_group_notice action.</summary>
internal sealed class GroupNoticeItem
{
    [JsonProperty("notice_id")]
    public string? NoticeId { get; set; }

    [JsonProperty("sender_id")]
    public long SenderId { get; set; }

    [JsonProperty("publish_time")]
    public long PublishTime { get; set; }

    [JsonProperty("message")]
    public GroupNoticeMessage? Message { get; set; }
}

/// <summary>Message content of a group notice.</summary>
internal sealed class GroupNoticeMessage
{
    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("images")]
    public List<GroupNoticeImage>? Images { get; set; }
}

/// <summary>Image in a group notice.</summary>
internal sealed class GroupNoticeImage
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("width")]
    public string? Width { get; set; }

    [JsonProperty("height")]
    public string? Height { get; set; }
}

/// <summary>Parameters for the _send_group_notice action.</summary>
internal sealed class SendGroupNoticeParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; } = "";

    [JsonProperty("image")]
    public string? Image { get; set; }

    [JsonProperty("confirm_required")]
    public bool ConfirmRequired { get; set; } = true;

    [JsonProperty("pinned")]
    public bool Pinned { get; set; }
}

/// <summary>Parameters for the _delete_group_notice action.</summary>
internal sealed class DeleteGroupNoticeParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("notice_id")]
    public string NoticeId { get; set; } = "";
}

/// <summary>Parameters for the get_essence_msg_list action.</summary>
internal sealed class GetEssenceMsgListParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }
}

/// <summary>Response item from the get_essence_msg_list action.</summary>
internal sealed class EssenceMsgItem
{
    [JsonProperty("message_id")]
    public int MessageId { get; set; }

    [JsonProperty("sender_id")]
    public long SenderId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("sender_nick")]
    public string? SenderNick { get; set; }

    [JsonProperty("operator_nick")]
    public string? OperatorNick { get; set; }

    [JsonProperty("sender_time")]
    public long SenderTime { get; set; }

    [JsonProperty("operator_time")]
    public long OperatorTime { get; set; }
}

/// <summary>Parameters for the set_essence_msg action.</summary>
internal sealed class SetEssenceMsgParams
{
    [JsonProperty("message_id")]
    public int MessageId { get; set; }
}

/// <summary>Response from the get_group_system_msg action.</summary>
internal sealed class GetGroupSystemMsgResponse
{
    [JsonProperty("invited_requests")]
    public List<GroupSystemMsgInvitedRequest>? InvitedRequests { get; set; }

    [JsonProperty("join_requests")]
    public List<GroupSystemMsgJoinRequest>? JoinRequests { get; set; }
}

/// <summary>Invited request item from group system messages.</summary>
internal sealed class GroupSystemMsgInvitedRequest
{
    [JsonProperty("request_id")]
    public long RequestId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("invitor_uin")]
    public long InvitorUin { get; set; }

    [JsonProperty("actor")]
    public long Actor { get; set; }

    [JsonProperty("group_name")]
    public string? GroupName { get; set; }

    [JsonProperty("invitor_nick")]
    public string? InvitorNick { get; set; }

    [JsonProperty("checked")]
    public bool Checked { get; set; }
}

/// <summary>Join request item from group system messages.</summary>
internal sealed class GroupSystemMsgJoinRequest
{
    [JsonProperty("request_id")]
    public long RequestId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("requester_uin")]
    public long RequesterUin { get; set; }

    [JsonProperty("actor")]
    public long Actor { get; set; }

    [JsonProperty("group_name")]
    public string? GroupName { get; set; }

    [JsonProperty("requester_nick")]
    public string? RequesterNick { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("checked")]
    public bool Checked { get; set; }
}

/// <summary>Parameters for the get_group_root_files action.</summary>
internal sealed class GetGroupRootFilesParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }
}

/// <summary>Parameters for the get_group_files_by_folder action.</summary>
internal sealed class GetGroupFilesByFolderParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("folder_id")]
    public string FolderId { get; set; } = "";
}

/// <summary>Response from get_group_root_files / get_group_files_by_folder actions.</summary>
internal sealed class GetGroupFilesResponse
{
    [JsonProperty("files")]
    public List<OB11GroupFile>? Files { get; set; }

    [JsonProperty("folders")]
    public List<OB11GroupFileFolder>? Folders { get; set; }
}

/// <summary>OB11 group file entry.</summary>
internal sealed class OB11GroupFile
{
    [JsonProperty("file_id")]
    public string? FileId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("uploader")]
    public long Uploader { get; set; }

    [JsonProperty("file_name")]
    public string? FileName { get; set; }

    [JsonProperty("uploader_name")]
    public string? UploaderName { get; set; }

    [JsonProperty("busid")]
    public int BusId { get; set; }

    [JsonProperty("file_size")]
    public long FileSize { get; set; }

    [JsonProperty("download_times")]
    public int DownloadTimes { get; set; }

    [JsonProperty("upload_time")]
    public long UploadTime { get; set; }

    [JsonProperty("modify_time")]
    public long ModifyTime { get; set; }

    [JsonProperty("dead_time")]
    public long DeadTime { get; set; }
}

/// <summary>OB11 group file folder entry.</summary>
internal sealed class OB11GroupFileFolder
{
    [JsonProperty("folder_id")]
    public string? FolderId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("creator")]
    public long Creator { get; set; }

    [JsonProperty("folder_name")]
    public string? FolderName { get; set; }

    [JsonProperty("creator_name")]
    public string? CreatorName { get; set; }

    [JsonProperty("total_file_count")]
    public int TotalFileCount { get; set; }

    [JsonProperty("create_time")]
    public long CreateTime { get; set; }
}

/// <summary>Parameters for the get_group_file_url action.</summary>
internal sealed class GetGroupFileUrlParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";

    [JsonProperty("busid")]
    public int BusId { get; set; }
}

/// <summary>Response from the get_group_file_url action.</summary>
internal sealed class GetGroupFileUrlResponse
{
    [JsonProperty("url")]
    public string? Url { get; set; }
}

/// <summary>Parameters for the delete_group_file action.</summary>
internal sealed class DeleteGroupFileParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";

    [JsonProperty("busid")]
    public int BusId { get; set; } = 102;
}

/// <summary>Parameters for the move_group_file action.</summary>
internal sealed class MoveGroupFileParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";

    [JsonProperty("parent_directory")]
    public string ParentDirectory { get; set; } = "";

    [JsonProperty("target_directory")]
    public string TargetDirectory { get; set; } = "";
}

/// <summary>Parameters for the rename_group_file action.</summary>
internal sealed class RenameGroupFileParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";

    [JsonProperty("new_name")]
    public string NewName { get; set; } = "";

    [JsonProperty("current_parent_directory")]
    public string CurrentParentDirectory { get; set; } = "";
}

/// <summary>Parameters for the create_group_file_folder action.</summary>
internal sealed class CreateGroupFileFolderParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("parent_id")]
    public string ParentId { get; set; } = "/";

    [JsonProperty("name")]
    public string Name { get; set; } = "";
}

/// <summary>Response from the create_group_file_folder action.</summary>
internal sealed class CreateGroupFileFolderResponse
{
    [JsonProperty("folder_id")]
    public string? FolderId { get; set; }
}

/// <summary>Parameters for the delete_group_folder action.</summary>
internal sealed class DeleteGroupFolderParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("folder_id")]
    public string FolderId { get; set; } = "";
}

/// <summary>Parameters for the rename_group_file_folder action.</summary>
internal sealed class RenameGroupFileFolderParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("folder_id")]
    public string FolderId { get; set; } = "";

    [JsonProperty("new_folder_name")]
    public string NewFolderName { get; set; } = "";
}

/// <summary>Parameters for the upload_group_file action.</summary>
internal sealed class UploadGroupFileParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("file")]
    public string File { get; set; } = "";

    [JsonProperty("folder")]
    public string Folder { get; set; } = "";
}

/// <summary>Parameters for the upload_private_file action.</summary>
internal sealed class UploadPrivateFileParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("file")]
    public string File { get; set; } = "";
}

/// <summary>Response from the upload_private_file action.</summary>
internal sealed class UploadPrivateFileResponse
{
    [JsonProperty("file_id")]
    public string? FileId { get; set; }
}

/// <summary>Parameters for the get_private_file_url action.</summary>
internal sealed class GetPrivateFileUrlParams
{
    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";
}

/// <summary>Response from the get_private_file_url action.</summary>
internal sealed class GetPrivateFileUrlResponse
{
    [JsonProperty("url")]
    public string? Url { get; set; }
}

/// <summary>Parameters for the delete_friend action.</summary>
internal sealed class DeleteFriendParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }
}

/// <summary>Parameters for the get_group_msg_history action.</summary>
internal sealed class GetGroupMsgHistoryParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("message_seq")]
    public int? MessageSeq { get; set; }

    [JsonProperty("count")]
    public int Count { get; set; } = 20;
}

/// <summary>Parameters for the get_friend_msg_history action.</summary>
internal sealed class GetFriendMsgHistoryParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("message_seq")]
    public int? MessageSeq { get; set; }

    [JsonProperty("count")]
    public int Count { get; set; } = 20;
}

/// <summary>Response from the get_group_msg_history / get_friend_msg_history actions.</summary>
internal sealed class GetMsgHistoryResponse
{
    [JsonProperty("messages")]
    public List<GetMsgResponse>? Messages { get; set; }
}

/// <summary>Parameters for the fetch_custom_face action.</summary>
internal sealed class FetchCustomFaceParams
{
    [JsonProperty("count")]
    public int Count { get; set; } = 48;
}

/// <summary>Parameters for the get_file action (for resource temp URL).</summary>
internal sealed class GetFileParams
{
    [JsonProperty("file_id")]
    public string? FileId { get; set; }

    [JsonProperty("file")]
    public string File { get; set; } = "";
}

/// <summary>Response from the get_file action.</summary>
internal sealed class GetFileResponse
{
    [JsonProperty("file_name")]
    public string? FileName { get; set; }

    [JsonProperty("file")]
    public string? File { get; set; }

    [JsonProperty("file_size")]
    public string? FileSize { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }
}

#region Extension API Parameters

/// <summary>Parameters for the forward_friend_single_msg / forward_group_single_msg action.</summary>
internal sealed class ForwardSingleMsgParams
{
    [JsonProperty("message_id")]
    public long MessageId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }
}

/// <summary>Response from the forward_friend_single_msg / forward_group_single_msg action.</summary>
internal sealed class ForwardSingleMsgResponse
{
    [JsonProperty("message_id")]
    public long MessageId { get; set; }
}

/// <summary>Parameters for the set_friend_remark action.</summary>
internal sealed class SetFriendRemarkParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("remark")]
    public string Remark { get; set; } = "";
}

/// <summary>Parameters for the get_group_shut_list action.</summary>
internal sealed class GetGroupShutListParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }
}

/// <summary>Parameters for the ocr_image action.</summary>
internal sealed class OcrImageParams
{
    [JsonProperty("image")]
    public string Image { get; set; } = "";
}

/// <summary>Response from the ocr_image action.</summary>
internal sealed class OcrImageResponse
{
    [JsonProperty("language")]
    public string? Language { get; set; }

    [JsonProperty("texts")]
    public List<OcrTextItem>? Texts { get; set; }
}

/// <summary>A single text detection item from OCR.</summary>
internal sealed class OcrTextItem
{
    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("confidence")]
    public int Confidence { get; set; }
}

/// <summary>Parameters for the voice_msg_to_text action.</summary>
internal sealed class VoiceMsgToTextParams
{
    [JsonProperty("message_id")]
    public long MessageId { get; set; }
}

/// <summary>Response from the voice_msg_to_text action.</summary>
internal sealed class VoiceMsgToTextResponse
{
    [JsonProperty("text")]
    public string? Text { get; set; }
}

/// <summary>Parameters for the set_online_status action.</summary>
internal sealed class SetOnlineStatusParams
{
    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("ext_status")]
    public int ExtStatus { get; set; }

    [JsonProperty("battery_status")]
    public int BatteryStatus { get; set; }
}

/// <summary>Parameters for the set_group_remark action.</summary>
internal sealed class SetGroupRemarkParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("remark")]
    public string Remark { get; set; } = "";
}

/// <summary>Parameters for the set_friend_category action.</summary>
internal sealed class SetFriendCategoryParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("category_id")]
    public int CategoryId { get; set; }
}

/// <summary>Response item from the get_friends_with_category action.</summary>
internal sealed class GetFriendsWithCategoryItem
{
    [JsonProperty("categoryId")]
    public int CategoryId { get; set; }

    [JsonProperty("categoryName")]
    public string? CategoryName { get; set; }

    [JsonProperty("categorySortId")]
    public int CategorySortId { get; set; }

    [JsonProperty("categoryMbCount")]
    public int CategoryMbCount { get; set; }

    [JsonProperty("onlineCount")]
    public int OnlineCount { get; set; }

    [JsonProperty("buddyList")]
    public List<GetFriendListItem>? BuddyList { get; set; }
}

#endregion