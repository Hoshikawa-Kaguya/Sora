namespace Sora.Enumeration;

/// <summary>
/// 指令匹配的消息来源
/// </summary>
public enum MessageSourceMatchFlag
{
    /// <summary>
    /// 全部
    /// </summary>
    All = 0,

    /// <summary>
    /// 群组
    /// </summary>
    Group = 1,

    /// <summary>
    /// 私信
    /// </summary>
    Private = 2
}