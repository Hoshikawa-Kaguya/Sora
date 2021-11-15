using System;
using Sora.Entities;
using Sora.Enumeration.EventParamsType;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 群成员数量变更事件参数
/// </summary>
public sealed class GroupMemberChangeEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 变更成员
    /// </summary>
    public User ChangedUser { get; private set; }

    /// <summary>
    /// 执行者
    /// </summary>
    public User Operator { get; private set; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public Group SourceGroup { get; private set; }

    /// <summary>
    /// 事件子类型
    /// </summary>
    public MemberChangeType SubType { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="groupMemberChangeArgs">群成员数量变更参数</param>
    internal GroupMemberChangeEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                        OnebotGroupMemberChangeEventArgs groupMemberChangeArgs) :
        base(serviceId, connectionId, eventName, groupMemberChangeArgs.SelfID, groupMemberChangeArgs.Time)
    {
        ChangedUser = new User(serviceId, connectionId, groupMemberChangeArgs.UserId);
        //执行者和变动成员可能为同一人
        Operator = groupMemberChangeArgs.UserId == groupMemberChangeArgs.OperatorId
            ? ChangedUser
            : new User(serviceId, connectionId, groupMemberChangeArgs.OperatorId);
        SourceGroup = new Group(serviceId, connectionId, groupMemberChangeArgs.GroupId);
        SubType     = groupMemberChangeArgs.SubType;
    }

    #endregion
}