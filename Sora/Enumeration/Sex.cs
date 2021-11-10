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
    [Description("")] 
    Male,

    /// <summary>
    /// 女
    /// </summary>
    [Description("")]
    Female,

    /// <summary>
    /// 未知
    /// </summary>
    [Description("")]
    Unknown
}