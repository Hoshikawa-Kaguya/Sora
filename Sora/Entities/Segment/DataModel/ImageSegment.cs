using Newtonsoft.Json;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 图片
/// </summary>
public sealed record ImageSegment : BaseSegment
{
    internal ImageSegment()
    {
    }

    #region 属性

    /// <summary>
    /// 文件名/绝对路径/URL/base64
    /// </summary>
    [JsonProperty(PropertyName = "file")]
    public string ImgFile { get; internal set; }

    /// <summary>
    /// 图片类型
    /// </summary>
    [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
    public string ImgType { get; internal set; }

    /// <summary>
    /// 图片链接
    /// </summary>
    [JsonProperty(PropertyName = "url", NullValueHandling = NullValueHandling.Ignore)]
    public string Url { get; internal set; }

    /// <summary>
    /// 只在通过网络 URL 发送时有效，表示是否使用已缓存的文件
    /// </summary>
    [JsonProperty(PropertyName = "cache", NullValueHandling = NullValueHandling.Ignore)]
    public int? UseCache { get; internal set; }

    /// <summary>
    /// 通过网络下载图片时的线程数，默认单线程。
    /// </summary>
    [JsonProperty(PropertyName = "c", NullValueHandling = NullValueHandling.Ignore)]
    public int? ThreadCount { get; internal set; }

    /// <summary>
    /// 发送秀图时的特效id，默认为40000
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public int? Id { get; internal set; }

    /// <summary>
    /// <para>图片子类型</para>
    /// <para>此类型暂只做反序列化处理，不做枚举类型处理</para>
    /// <para>参照：https://github.com/Mrs4s/go-cqhttp/blob/master/docs/cqhttp.md#%E5%9B%BE%E7%89%87</para>
    /// </summary>
    [JsonProperty(PropertyName = "subType", NullValueHandling = NullValueHandling.Ignore)]
    public int? SubType { get; internal set; }

    #endregion
}