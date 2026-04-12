using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Parameters for the get_stranger_info action.</summary>
internal sealed class GetStrangerInfoParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("no_cache")]
    public bool NoCache { get; set; }
}

/// <summary>Response from the get_stranger_info action.</summary>
internal sealed class GetStrangerInfoResponse
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("nickname")]
    public string? Nickname { get; set; }

    [JsonProperty("sex")]
    public string? Sex { get; set; }

    [JsonProperty("age")]
    public int Age { get; set; }
}

/// <summary>Represents a single item in the friend list.</summary>
internal sealed class GetFriendListItem
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("nickname")]
    public string? Nickname { get; set; }

    [JsonProperty("remark")]
    public string? Remark { get; set; }
}

/// <summary>Parameters for the send_like action.</summary>
internal sealed class SendLikeParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("times")]
    public int Times { get; set; } = 1;
}