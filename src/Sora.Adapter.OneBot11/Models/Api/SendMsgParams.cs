using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Parameters for the send_private_msg action.</summary>
internal sealed class SendPrivateMsgParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("message")]
    public object Message { get; set; } = new();

    [JsonProperty("auto_escape")]
    public bool AutoEscape { get; set; }
}

/// <summary>Parameters for the send_group_msg action.</summary>
internal sealed class SendGroupMsgParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("message")]
    public object Message { get; set; } = new();

    [JsonProperty("auto_escape")]
    public bool AutoEscape { get; set; }
}

/// <summary>Parameters for the send_group_forward_msg action.</summary>
internal sealed class SendGroupForwardMsgParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("messages")]
    public List<JObject> Messages { get; set; } = [];
}

/// <summary>Parameters for the send_private_forward_msg action.</summary>
internal sealed class SendPrivateForwardMsgParams
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("messages")]
    public List<JObject> Messages { get; set; } = [];
}

/// <summary>Response from the send_msg action.</summary>
internal sealed class SendMsgResponse
{
    [JsonProperty("message_id")]
    public int MessageId { get; set; }
}