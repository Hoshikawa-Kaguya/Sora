using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// At某人
/// </summary>
public sealed record AtSegment : BaseSegment
{
    internal AtSegment()
    {
    }

    #region 属性

    /// <summary>
    /// At目标UID
    /// 为<see langword="all"/>时为At全体
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "qq")]
    public string Target { get; internal set; }

    /// <summary>
    /// 覆盖被AT用户的用户名
    /// </summary>
    [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; internal set; }

    #endregion
}