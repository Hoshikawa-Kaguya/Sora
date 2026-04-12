using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Adapter.OneBot11.Models;

/// <summary>OneBot v11 message segment (array format).</summary>
internal sealed class OneBotSegment
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("data")]
    public JObject? Data { get; set; }
}