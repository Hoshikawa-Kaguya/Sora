using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Adapter.Milky.Models;

/// <summary>Milky event envelope.</summary>
internal sealed class MilkyEvent
{
    [JsonProperty("self_id")]
    public long SelfId { get; set; }

    [JsonProperty("event_type")]
    public string? EventType { get; set; }

    [JsonProperty("data")]
    public JObject? Data { get; set; }

    [JsonProperty("time")]
    public long Time { get; set; }
}