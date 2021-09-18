using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration;

namespace Sora.OnebotModel.ApiParams
{
    /// <summary>
    /// Onebot消息段
    /// </summary>
    internal readonly struct OnebotMessageElement
    {
        /// <summary>
        /// 消息段类型
        /// </summary>
        [JsonConverter(typeof(EnumDescriptionConverter))]
        [JsonProperty(PropertyName = "type")]
        internal SegmentType MsgType { get; init; }

        /// <summary>
        /// 消息段JSON
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        internal object RawData { get; init; }
    }
}