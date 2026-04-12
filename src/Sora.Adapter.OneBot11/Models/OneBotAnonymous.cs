using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models;

/// <summary>OneBot v11 anonymous user info.</summary>
internal sealed class OneBotAnonymous
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("flag")]
    public string? Flag { get; set; }
}