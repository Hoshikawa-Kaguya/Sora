using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Parameters for the set_friend_add_request action.</summary>
internal sealed class SetFriendAddRequestParams
{
    [JsonProperty("flag")]
    public string Flag { get; set; } = "";

    [JsonProperty("remark")]
    public string Remark { get; set; } = "";

    [JsonProperty("approve")]
    public bool Approve { get; set; } = true;
}

/// <summary>Parameters for the set_group_add_request action.</summary>
internal sealed class SetGroupAddRequestParams
{
    [JsonProperty("flag")]
    public string Flag { get; set; } = "";

    [JsonProperty("sub_type")]
    public string SubType { get; set; } = "add";

    [JsonProperty("reason")]
    public string Reason { get; set; } = "";

    [JsonProperty("approve")]
    public bool Approve { get; set; } = true;
}