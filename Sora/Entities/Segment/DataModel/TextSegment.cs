using Newtonsoft.Json;
using ProtoBuf;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 纯文本
/// </summary>
[ProtoContract]
public sealed record TextSegment : BaseSegment
{
    internal TextSegment()
    {
    }

#region 属性

    /// <summary>
    /// 纯文本内容
    /// </summary>
    [JsonProperty(PropertyName = "text")]
    [ProtoMember(1)]
    public string Content { get; internal set; }

#endregion
}