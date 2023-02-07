using Newtonsoft.Json;
using ProtoBuf;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 链接分享
/// </summary>
[ProtoContract]
public sealed record ShareSegment : BaseSegment
{
    internal ShareSegment()
    {
    }

#region 属性

    /// <summary>
    /// URL
    /// </summary>
    [JsonProperty(PropertyName = "url")]
    [ProtoMember(1)]
    public string Url { get; internal set; }

    /// <summary>
    /// 标题
    /// </summary>
    [JsonProperty(PropertyName = "title")]
    [ProtoMember(2)]
    public string Title { get; internal set; }

    /// <summary>
    /// 可选，内容描述
    /// </summary>
    [JsonProperty(PropertyName = "content", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(3)]
    public string Content { get; internal set; }

    /// <summary>
    /// 可选，图片 URL
    /// </summary>
    [JsonProperty(PropertyName = "image", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(4)]
    public string ImageUrl { get; internal set; }

#endregion
}