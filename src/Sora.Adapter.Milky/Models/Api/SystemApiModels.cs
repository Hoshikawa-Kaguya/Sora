using Newtonsoft.Json;

namespace Sora.Adapter.Milky.Models.Api;

/// <summary>Output data from the get_login_info API.</summary>
internal sealed class GetLoginInfoOutput
{
    [JsonProperty("uin")]
    public long Uin { get; set; }

    [JsonProperty("nickname")]
    public string? Nickname { get; set; }
}

/// <summary>Output data from the get_impl_info API.</summary>
internal sealed class GetImplInfoOutput
{
    [JsonProperty("impl_name")]
    public string? ImplName { get; set; }

    [JsonProperty("impl_version")]
    public string? ImplVersion { get; set; }

    [JsonProperty("milky_version")]
    public string? MilkyVersion { get; set; }

    [JsonProperty("qq_protocol_type")]
    public string? QqProtocolType { get; set; }

    [JsonProperty("qq_protocol_version")]
    public string? QqProtocolVersion { get; set; }
}

/// <summary>Input parameters for the get_user_profile API.</summary>
internal sealed class GetUserProfileInput
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }
}

/// <summary>Output data from the get_user_profile API.</summary>
internal sealed class GetUserProfileOutput
{
    [JsonProperty("nickname")]
    public string? Nickname { get; set; }

    [JsonProperty("qid")]
    public string? Qid { get; set; }

    [JsonProperty("remark")]
    public string? Remark { get; set; }

    [JsonProperty("sex")]
    public string? Sex { get; set; }

    [JsonProperty("bio")]
    public string? Bio { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }

    [JsonProperty("city")]
    public string? City { get; set; }

    [JsonProperty("school")]
    public string? School { get; set; }

    [JsonProperty("level")]
    public int Level { get; set; }

    [JsonProperty("age")]
    public int Age { get; set; }
}

/// <summary>Input with no_cache flag for list APIs.</summary>
internal sealed class NoCacheInput
{
    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}

/// <summary>Input parameters for the get_friend_info API.</summary>
internal sealed class GetFriendInfoInput
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}

/// <summary>Output data from the get_friend_list API.</summary>
internal sealed class GetFriendListOutput
{
    [JsonProperty("friends")]
    public List<MilkyFriendEntity> Friends { get; set; } = [];
}

/// <summary>Output data from the get_friend_info API.</summary>
internal sealed class GetFriendInfoOutput
{
    [JsonProperty("friend")]
    public MilkyFriendEntity Friend { get; set; } = new();
}

/// <summary>Input parameters for the get_group_info API.</summary>
internal sealed class GetGroupInfoInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}

/// <summary>Output data from the get_group_list API.</summary>
internal sealed class GetGroupListOutput
{
    [JsonProperty("groups")]
    public List<MilkyGroupEntity> Groups { get; set; } = [];
}

/// <summary>Output data from the get_group_info API.</summary>
internal sealed class GetGroupInfoOutput
{
    [JsonProperty("group")]
    public MilkyGroupEntity Group { get; set; } = new();
}

/// <summary>Input parameters for the get_group_member_list API.</summary>
internal sealed class GetGroupMemberListInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}

/// <summary>Output data from the get_group_member_list API.</summary>
internal sealed class GetGroupMemberListOutput
{
    [JsonProperty("members")]
    public List<MilkyGroupMemberEntity> Members { get; set; } = [];
}

/// <summary>Input parameters for the get_group_member_info API.</summary>
internal sealed class GetGroupMemberInfoInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}

/// <summary>Output data from the get_group_member_info API.</summary>
internal sealed class GetGroupMemberInfoOutput
{
    [JsonProperty("member")]
    public MilkyGroupMemberEntity Member { get; set; } = new();
}

/// <summary>Input parameters for the set_avatar API.</summary>
internal sealed class SetAvatarInput
{
    [JsonProperty("uri")]
    public string Uri { get; set; } = "";
}

/// <summary>Input parameters for the set_nickname API.</summary>
internal sealed class SetNicknameInput
{
    [JsonProperty("new_nickname")]
    public string NewNickname { get; set; } = "";
}

/// <summary>Input parameters for the set_bio API.</summary>
internal sealed class SetBioInput
{
    [JsonProperty("new_bio")]
    public string NewBio { get; set; } = "";
}

/// <summary>Output data from the get_custom_face_url_list API.</summary>
internal sealed class GetCustomFaceUrlListOutput
{
    [JsonProperty("urls")]
    public List<string> Urls { get; set; } = [];
}

/// <summary>Input parameters for the get_cookies API.</summary>
internal sealed class GetCookiesInput
{
    [JsonProperty("domain")]
    public string Domain { get; set; } = "";
}

/// <summary>Output data from the get_cookies API.</summary>
internal sealed class GetCookiesOutput
{
    [JsonProperty("cookies")]
    public string? Cookies { get; set; }
}

/// <summary>Output data from the get_csrf_token API.</summary>
internal sealed class GetCsrfTokenOutput
{
    [JsonProperty("csrf_token")]
    public string? CsrfToken { get; set; }
}

/// <summary>Output data from the get_peer_pins API.</summary>
internal sealed class GetPeerPinsOutput
{
    [JsonProperty("friends")]
    public List<MilkyFriendEntity> Friends { get; set; } = [];

    [JsonProperty("groups")]
    public List<MilkyGroupEntity> Groups { get; set; } = [];
}

/// <summary>Input parameters for the set_peer_pin API.</summary>
internal sealed class SetPeerPinInput
{
    [JsonProperty("peer_id")]
    public long PeerId { get; set; }

    [JsonProperty("message_scene")]
    public string MessageScene { get; set; } = "";

    [JsonProperty("is_pinned")]
    public bool IsPinned { get; set; }
}