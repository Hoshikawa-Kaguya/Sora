using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType;

/// <summary>
/// 管理员变动类型
/// </summary>
[DefaultValue(Unknown)]
public enum AdminChangeType
{
    /// <summary>
    /// 未知，在转换错误时为此值
    /// </summary>
    [Description("")] 
    Unknown,

    /// <summary>
    /// 设置
    /// </summary>
    [Description("set")] 
    Set,

    /// <summary>
    /// 取消
    /// </summary>
    [Description("unset")] 
    UnSet
}