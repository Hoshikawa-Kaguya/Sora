using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType;

/// <summary>
/// 精华信息变动类型
/// </summary>
[DefaultValue(Unknown)]
public enum EssenceChangeType
{
    /// <summary>
    /// 未知，在转换错误时为此值
    /// </summary>
    [Description("")] 
    Unknown,

    /// <summary>
    /// 添加
    /// </summary>
    [Description("add")]
    Add,

    /// <summary>
    /// 删除
    /// </summary>
    [Description("delete")]
    Delete
}