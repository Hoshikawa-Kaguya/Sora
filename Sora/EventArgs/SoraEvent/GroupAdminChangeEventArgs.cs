using System;
using Sora.Entities;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 管理员变动事件参数
/// </summary>
public sealed class GroupAdminChangeEventArgs : BaseSoraEventArgs
{
#region 属性

    /// <summary>
    /// 消息源群
    /// </summary>
    public Group SourceGroup { get; }

    /// <summary>
    /// 上传者
    /// </summary>
    public User Sender { get; }

    /// <summary>
    /// 动作类型
    /// </summary>
    public AdminChangeType SubType { get; }

#endregion

#region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="adminChangeArgs">管理员变动事件参数</param>
    internal GroupAdminChangeEventArgs(Guid                       serviceId,
                                       Guid                       connectionId,
                                       string                     eventName,
                                       OnebotAdminChangeEventArgs adminChangeArgs)
        : base(serviceId, connectionId, eventName, adminChangeArgs.SelfId, adminChangeArgs.Time, SourceFlag.Group)
    {
        SourceGroup = new Group(connectionId, adminChangeArgs.GroupId);
        Sender      = new User(serviceId, connectionId, adminChangeArgs.UserId);
        SubType     = adminChangeArgs.SubType;
    }

#endregion
}