using Newtonsoft.Json;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 接收红包
/// 仅支持GoCQ
/// </summary>
public sealed record RedbagSegment : BaseSegment
{
    internal RedbagSegment()
    {
    }

    /// <summary>
    /// 祝福语/口令
    /// </summary>
    [JsonProperty(PropertyName = "title")]
    public string Title { get; internal set; }
}