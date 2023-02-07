using Newtonsoft.Json;
using ProtoBuf;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 装逼大图
/// 仅支持GoCQ
/// </summary>
[ProtoContract]
public sealed record CardImageSegment : BaseSegment
{
    internal CardImageSegment()
    {
    }

    /// <summary>
    /// 文件名/绝对路径/URL/base64
    /// </summary>
    [JsonProperty(PropertyName = "file")]
    [ProtoMember(1)]
    public string ImageFile { get; internal set; }

    /// <summary>
    /// 最小width
    /// </summary>
    [JsonProperty(PropertyName = "minwidth")]
    [ProtoMember(2)]
    public long MinWidth { get; internal set; }

    /// <summary>
    /// 最小height
    /// </summary>
    [JsonProperty(PropertyName = "minheight")]
    [ProtoMember(3)]
    public long MinHeight { get; internal set; }

    /// <summary>
    /// 最大width
    /// </summary>
    [JsonProperty(PropertyName = "maxwidth")]
    [ProtoMember(4)]
    public long MaxWidth { get; internal set; }

    /// <summary>
    /// 最大height
    /// </summary>
    [JsonProperty(PropertyName = "maxheight")]
    [ProtoMember(5)]
    public long MaxHeight { get; internal set; }

    /// <summary>
    /// 来源名称
    /// </summary>
    [JsonProperty(PropertyName = "source")]
    [ProtoMember(6)]
    public string Source { get; internal set; }

    /// <summary>
    /// 来源图标url
    /// </summary>
    [JsonProperty(PropertyName = "icon")]
    [ProtoMember(7)]
    public string Icon { get; internal set; }
}