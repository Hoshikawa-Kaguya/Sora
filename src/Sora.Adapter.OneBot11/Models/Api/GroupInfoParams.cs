using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Parameters for the get_group_info action.</summary>
internal sealed class GetGroupInfoParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}

/// <summary>Parameters for the get_group_list action.</summary>
internal sealed class GetGroupListParams
{
    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}

/// <summary>Response from the get_group_info action.</summary>
internal sealed class GetGroupInfoResponse
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("owner_id")]
    public long OwnerId { get; set; }

    [JsonProperty("group_name")]
    public string? GroupName { get; set; }

    [JsonProperty("remark_name")]
    public string? RemarkName { get; set; }

    [JsonProperty("group_memo")]
    public string? GroupMemo { get; set; }

    [JsonProperty("member_count")]
    public int MemberCount { get; set; }

    [JsonProperty("max_member_count")]
    public int MaxMemberCount { get; set; }

    [JsonProperty("active_member_count")]
    public int? ActiveMemberCount { get; set; }

    [JsonProperty("group_create_time")]
    public long GroupCreateTime { get; set; }

    [JsonProperty("shut_up_all_timestamp")]
    public long ShutUpAllTimestamp { get; set; }

    [JsonProperty("shut_up_me_timestamp")]
    public long ShutUpMeTimestamp { get; set; }

    [JsonProperty("is_freeze")]
    public bool? IsFreeze { get; set; }

    [JsonProperty("is_top")]
    public bool IsTop { get; set; }
}

/// <summary>Parameters for the get_group_member_info action.</summary>
internal sealed class GetGroupMemberInfoParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}

/// <summary>Response from the get_group_member_info action.</summary>
internal sealed class GetGroupMemberInfoResponse
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

    [JsonProperty("area")]
    public string? Area { get; set; }

    [JsonProperty("role")]
    public string? Role { get; set; }

    [JsonProperty("sex")]
    public string? Sex { get; set; }

    [JsonProperty("level")]
    public string? Level { get; set; }

    [JsonProperty("age")]
    public int Age { get; set; }

    [JsonProperty("join_time")]
    public long JoinTime { get; set; }

    [JsonProperty("last_sent_time")]
    public long LastSentTime { get; set; }

    [JsonProperty("shut_up_timestamp")]
    public long ShutUpTimestamp { get; set; }

    [JsonProperty("title_expire_time")]
    public long TitleExpireTime { get; set; }

    [JsonProperty("card_changeable")]
    public bool CardChangeable { get; set; }

    [JsonProperty("unfriendly")]
    public bool Unfriendly { get; set; }
}

/// <summary>Parameters for the get_group_member_list action.</summary>
internal sealed class GetGroupMemberListParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}