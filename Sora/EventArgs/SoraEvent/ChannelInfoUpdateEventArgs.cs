using System;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.OnebotModel.ExtraEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 子频道信息更新
/// </summary>
public class ChannelInfoUpdateEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 更新前的频道信息
    /// </summary>
    public ChannelInfo OldChannelInfo { get; internal init; }

    /// <summary>
    /// 更新后的频道信息
    /// </summary>
    public ChannelInfo NewChannelInfo { get; internal init; }

    #endregion

    #region 构造方法

    internal ChannelInfoUpdateEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                        GocqChannelInfoUpdateEventArgs eventArgs) :
        base(serviceId, connectionId, eventName, eventArgs.SelfID, eventArgs.Time,
             new EventSource
             {
                 GuildId     = eventArgs.GuildId,
                 ChannelId   = eventArgs.ChannelId,
                 UserGuildId = eventArgs.UserGuildId
             })
    {
        OldChannelInfo = eventArgs.OldInfo;
        NewChannelInfo = eventArgs.NewInfo;
    }

    #endregion
}