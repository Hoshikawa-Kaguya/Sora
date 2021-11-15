using System;
using Sora.Entities;
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
    public Group SourceGroup { get; private set; }

    /// <summary>
    /// 上传者
    /// </summary>
    public User Sender { get; private set; }

    /// <summary>
    /// 动作类型
    /// </summary>
    public AdminChangeType SubType { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="adminChangeArgs">管理员变动事件参数</param>
    internal GroupAdminChangeEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                       OnebotAdminChangeEventArgs adminChangeArgs) :
        base(serviceId, connectionId, eventName, adminChangeArgs.SelfID, adminChangeArgs.Time)
    {
        SourceGroup = new Group(serviceId, connectionId, adminChangeArgs.GroupId);
        Sender      = new User(serviceId, connectionId, adminChangeArgs.UserId);
        SubType     = adminChangeArgs.SubType;
    }

    #endregion
}