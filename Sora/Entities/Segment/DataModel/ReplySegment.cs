using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 回复
/// </summary>
public sealed record ReplySegment : BaseSegment
{
    internal ReplySegment()
    {
    }

    #region 属性

    /// <summary>
    /// 消息ID
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "id")]
    public int Target { get; internal set; }

    #endregion
}