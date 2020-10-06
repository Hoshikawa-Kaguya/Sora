using System.Collections.Generic;
using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration;
using Sora.Model.Message;

namespace Sora.EventArgs.OnebotEvent.ApiEvent
{
    internal class SendMsgEventArgs : BaseApiMsgEventArgs
    {
        [JsonProperty(PropertyName = "params")]
        internal MsgData MessageData { get; set; }
    }

    internal class MsgData
    {
        [JsonConverter(typeof(EnumDescriptionConverter))]
        [JsonProperty(PropertyName = "message_type")]
        internal MessageType MessageType { get; set; }

        [JsonProperty(PropertyName = "user_id", NullValueHandling = NullValueHandling.Ignore)]
        internal long? UserId { get; set; } = null;

        [JsonProperty(PropertyName = "group_id", NullValueHandling = NullValueHandling.Ignore)]
        internal long? GroupId { get; set; } = null;

        [JsonProperty(PropertyName = "message")]
        internal List<OnebotMessage> Message { get; set; } = new List<OnebotMessage>();

        [JsonProperty(PropertyName = "auto_escape")]
        internal bool AutoEscape { get; set; } = false;
    }
}
