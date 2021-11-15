using System;
using Sora.Entities;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 群禁言事件参数
/// </summary>
public sealed class GroupMuteEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 被执行成员
    /// </summary>
    public User User { get; private set; }

    /// <summary>
    /// 执行者
    /// </summary>
    public User Operator { get; private set; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public Group SourceGroup { get; private set; }

    /// <summary>
    /// 禁言时长(s)
    /// </summary>
    internal long Duration { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="groupMuteArgs">群禁言事件参数</param>
    internal GroupMuteEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                OnebotGroupMuteEventArgs groupMuteArgs) :
        base(serviceId, connectionId, eventName, groupMuteArgs.SelfID, groupMuteArgs.Time)
    {
        User        = new User(serviceId, connectionId, groupMuteArgs.UserId);
        Operator    = new User(serviceId, connectionId, groupMuteArgs.OperatorId);
        SourceGroup = new Group(serviceId, connectionId, groupMuteArgs.GroupId);
        Duration    = groupMuteArgs.Duration;
    }

    #endregion
}