namespace Sora.Enumeration;

/// <summary>
/// 事件来源类型
/// </summary>
public enum SourceFlag
{
    /// <summary>
    /// 未知
    /// </summary>
    None = 0,

    /// <summary>
    /// 群组
    /// </summary>
    Group = 1,

    /// <summary>
    /// 用户
    /// </summary>
    Private = 2,

    /// <summary>
    /// 系统消息
    /// </summary>
    System = 3
}