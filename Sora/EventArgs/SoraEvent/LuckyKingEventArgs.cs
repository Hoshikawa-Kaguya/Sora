using System;
using Sora.Entities;
using Sora.Enumeration;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 红包运气王事件参数
/// </summary>
public sealed class LuckyKingEventArgs : BaseSoraEventArgs
{
#region 属性

    /// <summary>
    /// 红包发送者
    /// </summary>
    public User SendUser { get; }

    /// <summary>
    /// 运气王
    /// </summary>
    public User TargetUser { get; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public Group SourceGroup { get; }

#endregion

#region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="luckyKingEventArgs">运气王事件参数</param>
    internal LuckyKingEventArgs(Guid                       serviceId,
                                Guid                       connectionId,
                                string                     eventName,
                                OnebotPokeOrLuckyEventArgs luckyKingEventArgs)
        : base(serviceId, connectionId, eventName, luckyKingEventArgs.SelfId, luckyKingEventArgs.Time, SourceFlag.Group)
    {
        SendUser    = new User(serviceId, connectionId, luckyKingEventArgs.UserId);
        TargetUser  = new User(serviceId, connectionId, luckyKingEventArgs.TargetId);
        SourceGroup = new Group(connectionId, luckyKingEventArgs.GroupId);
    }

#endregion
}