using Newtonsoft.Json;
using ProtoBuf;
using Sora.Converter;
using Sora.Enumeration;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// <para>Xml与Json集合</para>
/// <para>可能为<see cref="SegmentType.Json"/>或<see cref="SegmentType.Xml"/></para>
/// </summary>
[ProtoContract]
public sealed record CodeSegment : BaseSegment
{
    internal CodeSegment()
    {
    }

#region 属性

    /// <summary>
    /// 内容
    /// </summary>
    [JsonProperty(PropertyName = "data")]
    [ProtoMember(1)]
    public string Content { get; internal set; }

    /// <summary>
    /// 是否走富文本通道
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "resid", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(2)]
    public int? Resid { get; internal set; }

#endregion
}