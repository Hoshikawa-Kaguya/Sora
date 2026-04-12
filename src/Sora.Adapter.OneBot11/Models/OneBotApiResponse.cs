using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Adapter.OneBot11.Models;

/// <summary>OneBot v11 API response envelope.</summary>
internal sealed class OneBotApiResponse
{
    [JsonProperty("echo")]
    public string? Echo { get; set; }

    [JsonProperty("retcode")]
    public int RetCode { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("data")]
    public JToken? Data { get; set; }
}