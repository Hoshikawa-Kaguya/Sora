using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Model
{
    internal class OnebotMessage
    {
        [JsonProperty(PropertyName = "type")]
        internal string MsgType { get; set; }

        [JsonProperty(PropertyName = "data")]
        internal JObject MsgData { get; set; }
    }
}
