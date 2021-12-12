using System;
using Sora.Entities;
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
        //Message = 
    }
}