using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Converter;
using Sora.Enumeration;

namespace Sora.Model.Message
{
    /// <summary>
    /// Onebot消息段
    /// </summary>
    public class OnebotMessage
    {
        /// <summary>
        /// 消息段类型
        /// </summary>
        [JsonConverter(typeof(EnumDescriptionConverter))]
        [JsonProperty(PropertyName = "type")]
        public CQFunction MsgType { get; set; }

        /// <summary>
        /// 消息段JSON
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        internal JObject RawData { get; set; }
    }
}
