using System;
using Sora.Entities;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 成员名片变更事件参数
/// </summary>
public sealed class GroupCardUpdateEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 名片改变的成员
    /// </summary>
    public User User { get; private set; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public Group SourceGroup { get; private set; }

    /// <summary>
    /// 新名片
    /// </summary>
    public string NewCard { get; private set; }

    /// <summary>
    /// 旧名片
    /// </summary>
    public string OldCard { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="groupCardUpdateArgs">群名片更新事件参数</param>
    internal GroupCardUpdateEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                      OnebotGroupCardUpdateEventArgs groupCardUpdateArgs) :
        base(serviceId, connectionId, eventName, groupCardUpdateArgs.SelfID, groupCardUpdateArgs.Time)
    {
        User        = new User(serviceId, connectionId, groupCardUpdateArgs.UserId);
        SourceGroup = new Group(serviceId, connectionId, groupCardUpdateArgs.GroupId);
        NewCard     = groupCardUpdateArgs.NewCard;
        OldCard     = groupCardUpdateArgs.OldCard;
    }

    #endregion
}