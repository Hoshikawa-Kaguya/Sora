namespace Sora.Entities;

/// <summary>
/// 消息来源信息
/// </summary>
public sealed record EventSource
{
    /// <summary>
    /// 事件源用户ID
    /// </summary>
    public long? UserId { get; internal init; }

    /// <summary>
    /// 事件源频道用户ID
    /// </summary>
    public ulong? UserGuildId { get; internal init; }

    /// <summary>
    /// 事件源群ID
    /// </summary>
    public long? GroupId { get; internal init; }

    /// <summary>
    /// 事件源频道ID
    /// </summary>
    public ulong? GuildId { get; internal init; }

    /// <summary>
    /// 事件源子频道ID
    /// </summary>
    public ulong? ChannelId { get; internal init; }
}