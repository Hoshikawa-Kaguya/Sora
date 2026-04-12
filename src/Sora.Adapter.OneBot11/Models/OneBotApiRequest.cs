using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models;

/// <summary>OneBot v11 API request envelope.</summary>
internal sealed class OneBotApiRequest
{
    [JsonProperty("echo")]
    public string Echo { get; set; } = "";

    [JsonProperty("action")]
    public string Action { get; set; } = "";

    [JsonProperty("params")]
    public object Params { get; set; } = new();
}