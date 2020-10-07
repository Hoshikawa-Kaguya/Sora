using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.ApiEvent
{
    internal class GetForwardMessageArgs : BaseApiMsgEventArgs
    {
        [JsonProperty(PropertyName = "params")]
        internal ForwardData Forward { get; set; }
    }

    internal class ForwardData
    {
        [JsonProperty(PropertyName = "message_id")]
        internal string MessageId { get; set; }
    }
}
