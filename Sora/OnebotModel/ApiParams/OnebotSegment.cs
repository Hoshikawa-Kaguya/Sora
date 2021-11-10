using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Converter;
using Sora.Enumeration;

namespace Sora.OnebotModel.ApiParams;

/// <summary>
/// Onebot消息段
/// </summary>
internal readonly struct OnebotSegment
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
    internal JToken RawData { get; init; }
}