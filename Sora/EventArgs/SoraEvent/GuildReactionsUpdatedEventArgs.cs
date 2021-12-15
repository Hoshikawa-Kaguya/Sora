using System;
using System.Collections.Generic;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.OnebotModel.ExtraEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 频道消息表情贴更新
/// </summary>
public class GuildReactionsUpdatedEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// <para>消息ID</para>
    /// </summary>
    public int MessageId { get; internal init; }

    /// <summary>
    /// 作用不明
    /// </summary>
    public ulong MessageSenderId { get; internal init; }

    /// <summary>
    /// 当前消息被贴表情列表
    /// </summary>
    public List<ReactionInfo> Reactions { get; internal init; }

    #endregion


    internal GuildReactionsUpdatedEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                            GocqReactionsUpdatedEventArgs eventArgs)
        : base(serviceId, connectionId, eventName, eventArgs.SelfID, eventArgs.Time,
               new EventSource
               {
                   GuildId     = eventArgs.GuildId,
                   ChannelId   = eventArgs.ChannelId,
                   UserGuildId = eventArgs.UserGuildId
               })
    {
        MessageId       = eventArgs.MessageId;
        MessageSenderId = eventArgs.MessageSenderId;
        Reactions       = eventArgs.Reactions;
    }
}