using Newtonsoft.Json;
using ProtoBuf;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 合并转发/合并转发节点
/// </summary>
[ProtoContract]
public sealed record ForwardSegment : BaseSegment
{
    internal ForwardSegment()
    {
    }

#region 属性

    /// <summary>
    /// 转发消息ID
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    [ProtoMember(1)]
    public string MessageId { get; internal set; }

#endregion
}