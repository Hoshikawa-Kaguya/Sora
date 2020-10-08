using Newtonsoft.Json;

namespace Sora.Model.OnebotApi
{
    internal class GetForwardParams
    {
        [JsonProperty(PropertyName = "message_id")]
        internal string MessageId { get; set; }
    }
}
