using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Adapter.Milky.Models;

/// <summary>Milky API response envelope.</summary>
internal sealed class MilkyApiResponse
{
    [JsonProperty("retcode")]
    public int RetCode { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("data")]
    public JToken? Data { get; set; }
}