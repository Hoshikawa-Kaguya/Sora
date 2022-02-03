using System.ComponentModel;

namespace Sora.Enumeration;

/// <summary>
/// 性别
/// </summary>
[DefaultValue(Unknown)]
public enum Sex
{
    /// <summary>
    /// 男
    /// </summary>
    [Description("male")]
    Male,

    /// <summary>
    /// 女
    /// </summary>
    [Description("female")]
    Female,

    /// <summary>
    /// 未知
    /// </summary>
    [Description("unknown")]
    Unknown
}