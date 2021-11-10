using Newtonsoft.Json.Linq;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 未知消息段
/// </summary>
public sealed record UnknownSegment : BaseSegment
{
    internal UnknownSegment()
    {
    }

    #region 属性

    /// <summary>
    /// 纯文本内容
    /// </summary>
    public JObject Content { get; internal set; }

    #endregion
}