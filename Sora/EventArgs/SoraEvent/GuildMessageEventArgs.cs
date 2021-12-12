using System;
using Sora.Converter;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.OnebotModel.ExtraEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 频道消息事件参数
/// </summary>
public class GuildMessageEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 消息内容
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// 消息发送者实例
    /// </summary>
    public GuildUser Sender { get; }

    /// <summary>
    /// 发送者信息
    /// </summary>
    public GuildSenderInfo SenderInfo { get; }

    /// <summary>
    /// 源频道
    /// </summary>
    public Guild SourceGuild { get; }

    /// <summary>
    /// 源子频道
    /// </summary>
    public Channel SourceChannel { get; }

    /// <summary>
    /// 登陆账号的频道ID
    /// </summary>
    public long SelfGuildId { get; }

    #endregion

    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">链接ID</param>
    /// <param name="eventName">事件名</param>
    /// <param name="guildMsgArgs">原始事件参数</param>
    internal GuildMessageEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                   GocqGuildMessageEventArgs guildMsgArgs) :
        base(serviceId, connectionId, eventName, guildMsgArgs.SelfID, guildMsgArgs.Time)
    {
        Message = new Message(serviceId, connectionId, guildMsgArgs.MessageId, string.Empty,
                              guildMsgArgs.MessageList.ToMessageBody(), guildMsgArgs.Time, 0, null);
        Sender        = new GuildUser(serviceId, connectionId, guildMsgArgs.UserId);
        SenderInfo    = guildMsgArgs.SenderInfo;
        SourceGuild   = new Guild(serviceId, connectionId, guildMsgArgs.GuildId);
        SourceChannel = new Channel(serviceId, connectionId, guildMsgArgs.ChannelId);
        SelfGuildId   = guildMsgArgs.SelfTinyId;
    }
}