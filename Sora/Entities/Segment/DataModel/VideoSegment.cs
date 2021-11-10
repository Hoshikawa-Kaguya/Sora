using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 短视频
/// </summary>
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
    public string VideoFile { get; internal set; }

    /// <summary>
    /// 视频 URL
    /// </summary>
    [JsonProperty(PropertyName = "url", NullValueHandling = NullValueHandling.Ignore)]
    public string Url { get; internal set; }

    /// <summary>
    /// 是否使用已缓存的文件
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "cache", NullValueHandling = NullValueHandling.Ignore)]
    public int? Cache { get; internal set; }

    /// <summary>
    /// 是否使用已缓存的文件
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "proxy", NullValueHandling = NullValueHandling.Ignore)]
    public int? Proxy { get; internal set; }

    /// <summary>
    /// 是否使用已缓存的文件
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "timeout", NullValueHandling = NullValueHandling.Ignore)]
    public int? Timeout { get; internal set; }

    #endregion
}