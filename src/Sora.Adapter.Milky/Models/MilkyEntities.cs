using Newtonsoft.Json;

namespace Sora.Adapter.Milky.Models;

/// <summary>Milky friend entity DTO.</summary>
internal sealed class MilkyFriendEntity
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("nickname")]
    public string? Nickname { get; set; }

    [JsonProperty("qid")]
    public string? Qid { get; set; }

    [JsonProperty("remark")]
    public string? Remark { get; set; }

    [JsonProperty("sex")]
    public string? Sex { get; set; }

    [JsonProperty("category")]
    public MilkyFriendCategoryEntity? Category { get; set; }
}

/// <summary>Milky friend category entity DTO.</summary>
internal sealed class MilkyFriendCategoryEntity
{
    [JsonProperty("category_id")]
    public int CategoryId { get; set; }

    [JsonProperty("category_name")]
    public string? CategoryName { get; set; }
}

/// <summary>Milky group entity DTO.</summary>
internal sealed class MilkyGroupEntity
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("group_name")]
    public string? GroupName { get; set; }

    [JsonProperty("remark")]
    public string? Remark { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("question")]
    public string? Question { get; set; }

    [JsonProperty("announcement")]
    public string? Announcement { get; set; }

    [JsonProperty("member_count")]
    public int MemberCount { get; set; }

    [JsonProperty("max_member_count")]
    public int MaxMemberCount { get; set; }

    [JsonProperty("created_time")]
    public long CreatedTime { get; set; }
}

/// <summary>Milky group member entity DTO.</summary>
internal sealed class MilkyGroupMemberEntity
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("nickname")]
    public string? Nickname { get; set; }

    [JsonProperty("card")]
    public string? Card { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("role")]
    public string? Role { get; set; }

    [JsonProperty("sex")]
    public string? Sex { get; set; }

    [JsonProperty("level")]
    public int Level { get; set; }

    [JsonProperty("join_time")]
    public long JoinTime { get; set; }

    [JsonProperty("last_sent_time")]
    public long LastSentTime { get; set; }

    [JsonProperty("shut_up_end_time")]
    public long ShutUpEndTime { get; set; }
}

/// <summary>Milky group announcement entity DTO.</summary>
internal sealed class MilkyGroupAnnouncementEntity
{
    [JsonProperty("announcement_id")]
    public string? AnnouncementId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("content")]
    public string? Content { get; set; }

    [JsonProperty("image_url")]
    public string? ImageUrl { get; set; }

    [JsonProperty("time")]
    public long Time { get; set; }
}

/// <summary>Milky group file entity DTO.</summary>
internal sealed class MilkyGroupFileEntity
{
    [JsonProperty("file_id")]
    public string? FileId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("uploader_id")]
    public long UploaderId { get; set; }

    [JsonProperty("file_name")]
    public string? FileName { get; set; }

    [JsonProperty("parent_folder_id")]
    public string? ParentFolderId { get; set; }

    [JsonProperty("file_size")]
    public long FileSize { get; set; }

    [JsonProperty("downloaded_times")]
    public int DownloadedTimes { get; set; }

    [JsonProperty("uploaded_time")]
    public long UploadedTime { get; set; }

    [JsonProperty("expire_time")]
    public long ExpireTime { get; set; }
}

/// <summary>Milky group folder entity DTO.</summary>
internal sealed class MilkyGroupFolderEntity
{
    [JsonProperty("folder_id")]
    public string? FolderId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("creator_id")]
    public long CreatorId { get; set; }

    [JsonProperty("folder_name")]
    public string? FolderName { get; set; }

    [JsonProperty("parent_folder_id")]
    public string? ParentFolderId { get; set; }

    [JsonProperty("file_count")]
    public int FileCount { get; set; }

    [JsonProperty("created_time")]
    public long CreatedTime { get; set; }

    [JsonProperty("last_modified_time")]
    public long LastModifiedTime { get; set; }
}

/// <summary>Milky group essence message entity DTO.</summary>
internal sealed class MilkyGroupEssenceMessage
{
    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("sender_id")]
    public long SenderId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("sender_name")]
    public string? SenderName { get; set; }

    [JsonProperty("operator_name")]
    public string? OperatorName { get; set; }

    [JsonProperty("segments")]
    public List<MilkySegment> Segments { get; set; } = [];

    [JsonProperty("message_time")]
    public long MessageTime { get; set; }

    [JsonProperty("operation_time")]
    public long OperationTime { get; set; }
}