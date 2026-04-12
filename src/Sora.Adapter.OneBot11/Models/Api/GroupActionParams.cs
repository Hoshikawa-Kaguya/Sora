using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Parameters for the set_group_kick action.</summary>
internal sealed class SetGroupKickParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("reject_add_request")]
    public bool RejectAddRequest { get; set; }
}

/// <summary>Parameters for the set_group_ban action.</summary>
internal sealed class SetGroupBanParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("duration")]
    public int Duration { get; set; } = 1800;
}

/// <summary>Parameters for the set_group_whole_ban action.</summary>
internal sealed class SetGroupWholeBanParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("enable")]
    public bool Enable { get; set; } = true;
}

/// <summary>Parameters for the set_group_admin action.</summary>
internal sealed class SetGroupAdminParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("enable")]
    public bool Enable { get; set; } = true;
}

/// <summary>Parameters for the set_group_card action.</summary>
internal sealed class SetGroupCardParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("card")]
    public string Card { get; set; } = "";
}

/// <summary>Parameters for the set_group_name action.</summary>
internal sealed class SetGroupNameParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("group_name")]
    public string GroupName { get; set; } = "";
}

/// <summary>Parameters for the set_group_leave action.</summary>
internal sealed class SetGroupLeaveParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("is_dismiss")]
    public bool IsDismiss { get; set; }
}

/// <summary>Parameters for the set_group_special_title action.</summary>
internal sealed class SetGroupSpecialTitleParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("special_title")]
    public string SpecialTitle { get; set; } = "";

    [JsonProperty("duration")]
    public int Duration { get; set; } = -1;
}

/// <summary>Parameters for the set_group_anonymous_ban action.</summary>
internal sealed class SetGroupAnonymousBanParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("anonymous_flag")]
    public string AnonymousFlag { get; set; } = "";

    [JsonProperty("duration")]
    public int Duration { get; set; } = 1800;
}

/// <summary>Parameters for the set_group_anonymous action.</summary>
internal sealed class SetGroupAnonymousParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("enable")]
    public bool Enable { get; set; } = true;
}