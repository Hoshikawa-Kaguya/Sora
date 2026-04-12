using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models;

/// <summary>OneBot v11 message sender info.</summary>
internal sealed class OneBotSender
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("nickname")]
    public string? Nickname { get; set; }

    [JsonProperty("card")]
    public string? Card { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("role")]
    public string? Role { get; set; }

    [JsonProperty("sex")]
    public string? Sex { get; set; }

    [JsonProperty("level")]
    public string? Level { get; set; }

    [JsonProperty("age")]
    public int Age { get; set; }

    [JsonProperty("area")]
    public string? Area { get; set; }
}