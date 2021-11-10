using Newtonsoft.Json;

namespace Sora.Entities;

/// <summary>
/// 二维向量
/// </summary>
public sealed class Vector2
{
    #region 属性

    /// <summary>
    /// X
    /// </summary>
    [JsonProperty(PropertyName = "x")]
    public int X { get; internal init; }

    /// <summary>
    /// Y
    /// </summary>
    [JsonProperty(PropertyName = "y")]
    public int Y { get; internal init; }

    #endregion

    #region 构造方法

    internal Vector2()
    {
    }

    #endregion
}