using System;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Enumeration;
using Sora.OnebotModel.ExtraEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 频道创建/删除事件参数
/// </summary>
public class ChannelChangeEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 频道信息
    /// </summary>
    public ChannelInfo ChannelInfo { get; internal init; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public ChannelChangeType ChannelChangeType { get; internal init; }

    #endregion

    #region 构造方法

    internal ChannelChangeEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                    GocqChannelCreatedOrDestroyedEventArgs eventArgs, ChannelChangeType changeType) :
        base(serviceId, connectionId, eventName, eventArgs.SelfID, eventArgs.Time,
             new EventSource
             {
                 GuildId     = eventArgs.GuildId,
                 ChannelId   = eventArgs.ChannelId,
                 UserGuildId = eventArgs.UserGuildId
             })
    {
        ChannelInfo       = eventArgs.ChannelInfo;
        ChannelChangeType = changeType;
    }

    #endregion
}