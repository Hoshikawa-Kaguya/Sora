using Newtonsoft.Json;
using ProtoBuf;
using Sora.Converter;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 短视频
/// </summary>
[ProtoContract]
public sealed record VideoSegment : BaseSegment
{
    internal VideoSegment()
    {
    }

#region 属性

    /// <summary>
    /// 视频文件名
    /// </summary>
    [JsonProperty(PropertyName = "file")]
    [ProtoMember(1)]
    public string VideoFile { get; internal set; }

    /// <summary>
    /// 视频 URL
    /// </summary>
    [JsonProperty(PropertyName = "url", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(2)]
    public string Url { get; internal set; }

    /// <summary>
    /// 是否使用已缓存的文件
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "cache", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(3)]
    public int? Cache { get; internal set; }

    /// <summary>
    /// 是否使用已缓存的文件
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "proxy", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(4)]
    public int? Proxy { get; internal set; }

    /// <summary>
    /// 是否使用已缓存的文件
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "timeout", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(5)]
    public int? Timeout { get; internal set; }

#endregion
}