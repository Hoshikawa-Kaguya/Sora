using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Parameters for the get_forward_msg action.</summary>
internal sealed class GetForwardMsgParams
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";
}

/// <summary>Response from the get_forward_msg action.</summary>
internal sealed class GetForwardMsgResponse
{
    [JsonProperty("messages")]
    public List<ForwardMsgNode>? Messages { get; set; }
}

/// <summary>A single node in a forwarded message list.</summary>
internal sealed class ForwardMsgNode
{
    [JsonProperty("message_format")]
    public string? MessageFormat { get; set; }

    [JsonProperty("content")]
    public JToken? Content { get; set; }

    [JsonProperty("time")]
    public long Time { get; set; }

    [JsonProperty("sender")]
    public ForwardMsgSender? Sender { get; set; }
}

/// <summary>Sender info in a forwarded message node.</summary>
internal sealed class ForwardMsgSender
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("nickname")]
    public string? Nickname { get; set; }
}