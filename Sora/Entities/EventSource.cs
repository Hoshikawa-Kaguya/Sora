namespace Sora.Entities;

/// <summary>
/// 消息来源信息
/// </summary>
public record EventSource
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; internal init; }

    /// <summary>
    /// 频道用户ID
    /// </summary>
    public long? UserGuildId { get; internal init; }

    /// <summary>
    /// 群ID
    /// </summary>
    public long? GroupId { get; internal init; }

    /// <summary>
    /// 频道ID
    /// </summary>
    public long? GuildId { get; internal init; }

    /// <summary>
    /// 子频道ID
    /// </summary>
    public long? ChannelId { get; internal init; }
}