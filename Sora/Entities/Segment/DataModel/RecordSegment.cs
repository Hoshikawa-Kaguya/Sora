using Newtonsoft.Json;
using ProtoBuf;
using Sora.Converter;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 语音消息
/// </summary>
[ProtoContract]
public sealed record RecordSegment : BaseSegment
{
    internal RecordSegment()
    {
    }

#region 属性

    /// <summary>
    /// 文件名/绝对路径/URL/base64
    /// </summary>
    [JsonProperty(PropertyName = "file")]
    [ProtoMember(1)]
    public string RecordFile { get; internal set; }

    /// <summary>
    /// 表示变声
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "magic")]
    [ProtoMember(2)]
    public int? Magic { get; internal set; }

    /// <summary>
    /// 语音 URL
    /// </summary>
    [JsonProperty(PropertyName = "url", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(3)]
    public string Url { get; internal set; }

    /// <summary>
    /// 是否使用已缓存的文件
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "cache", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(4)]
    public int? Cache { get; internal set; }

    /// <summary>
    /// 是否使用代理
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "proxy", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(5)]
    public int? Proxy { get; internal set; }

    /// <summary>
    /// 是否使用已缓存的文件
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "timeout", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(6)]
    public int? Timeout { get; internal set; }

#endregion
}