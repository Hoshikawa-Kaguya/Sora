using System;
using System.Threading.Tasks;
using Sora.Entities.Info;

namespace Sora.Entities;

//TODO 完善相关方法
/// <summary>
/// 子频道实例
/// </summary>
public sealed class Channel : Guild
{
    #region 属性

    /// <summary>
    /// 子频道ID
    /// </summary>
    public ulong ChannelId { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器连接标识</param>
    /// <param name="cid">子频道ID</param>
    /// <param name="gid">频道ID</param>
    internal Channel(Guid serviceId, Guid connectionId, ulong cid, ulong gid) : base(serviceId, connectionId, gid)
    {
        ChannelId = cid;
    }

    #endregion

    #region 消息方法

    /// <summary>
    /// 发送子频道消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="timeout">覆盖原有超时</param>
    public async ValueTask<(ApiStatus apiStatus, string messageId)> SendChannelMessage(
        MessageBody message, TimeSpan? timeout = null)
    {
        return await SoraApi.SendGuildMessage(GuildId, ChannelId, message, timeout);
    }

    #endregion

    #region 转换方法

    /// <summary>
    /// 定义将 <see cref="User"/> 对象转换为 <see cref="ulong"/>
    /// </summary>
    /// <param name="value">转换的 <see cref="User"/> 对象</param>
    public static implicit operator ulong(Channel value)
    {
        return value.ChannelId;
    }

    /// <summary>
    /// 定义将 <see cref="User"/> 对象转换为 <see cref="string"/>
    /// </summary>
    /// <param name="value">转换的 <see cref="User"/> 对象</param>
    public static implicit operator string(Channel value)
    {
        return value.ToString();
    }

    #endregion
}