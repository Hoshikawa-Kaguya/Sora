using System;

namespace Sora.Enumeration;

/// <summary>
/// 事件来源类型
/// </summary>
[Flags]
public enum SourceFlag
{
    /// <summary>
    /// 群组
    /// </summary>
    Group = 1 << 0,

    /// <summary>
    /// 用户
    /// </summary>
    Private = 1 << 1,

    /// <summary>
    /// 系统消息
    /// </summary>
    System = 1 << 2
}