using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Parameters for the get_msg action.</summary>
internal sealed class GetMsgParams
{
    [JsonProperty("message_id")]
    public int MessageId { get; set; }
}

/// <summary>Response from the get_msg action.</summary>
internal sealed class GetMsgResponse
{
    [JsonProperty("message_id")]
    public int MessageId { get; set; }

    [JsonProperty("real_id")]
    public int RealId { get; set; }

    [JsonProperty("message_type")]
    public string? MessageType { get; set; }

    [JsonProperty("message")]
    public JToken? Message { get; set; }

    [JsonProperty("time")]
    public long Time { get; set; }

    [JsonProperty("sender")]
    public OneBotSender? Sender { get; set; }
}