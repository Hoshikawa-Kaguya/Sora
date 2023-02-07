using Newtonsoft.Json;
using ProtoBuf;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 接收红包
/// 仅支持GoCQ
/// </summary>
[ProtoContract]
public sealed record RedbagSegment : BaseSegment
{
    internal RedbagSegment()
    {
    }

    /// <summary>
    /// 祝福语/口令
    /// </summary>
    [JsonProperty(PropertyName = "title")]
    [ProtoMember(1)]
    public string Title { get; internal set; }
}