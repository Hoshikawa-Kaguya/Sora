using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Parameters for the delete_msg action.</summary>
internal sealed class DeleteMsgParams
{
    [JsonProperty("message_id")]
    public int MessageId { get; set; }
}