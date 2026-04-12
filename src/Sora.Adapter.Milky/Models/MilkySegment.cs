using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Adapter.Milky.Models;

/// <summary>Milky message segment.</summary>
internal sealed class MilkySegment
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("data")]
    public JObject? Data { get; set; }
}