using System;
using Sora.Entities;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 群消息撤回事件参数
/// </summary>
public sealed class GroupRecallEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 消息发送者
    /// </summary>
    public User MessageSender { get; private set; }

    /// <summary>
    /// 撤回执行者
    /// </summary>
    public User Operator { get; private set; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public Group SourceGroup { get; private set; }

    /// <summary>
    /// 被撤消息ID
    /// </summary>
    public int MessageId { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="groupRecallArgs">群聊撤回事件参数</param>
    internal GroupRecallEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                  ApiGroupRecallEventArgs groupRecallArgs) :
        base(serviceId, connectionId, eventName, groupRecallArgs.SelfID, groupRecallArgs.Time)
    {
        MessageSender = new User(serviceId, connectionId, groupRecallArgs.UserId);
        //执行者和发送者可能是同一人
        Operator = groupRecallArgs.UserId == groupRecallArgs.OperatorId
            ? MessageSender
            : new User(serviceId, connectionId, groupRecallArgs.OperatorId);
        SourceGroup = new Group(serviceId, connectionId, groupRecallArgs.GroupId);
        MessageId   = groupRecallArgs.MessageId;
    }

    #endregion
}