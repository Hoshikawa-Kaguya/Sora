using System;
using Sora.Entities;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 授予荣誉事件参数
/// </summary>
public sealed class HonorEventArgs : BaseSoraEventArgs
{
#region 属性

    /// <summary>
    /// 荣誉获得者
    /// </summary>
    public User TargetUser { get; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public Group SourceGroup { get; }

    /// <summary>
    /// 荣誉类型
    /// </summary>
    public HonorType Honor { get; }

#endregion

#region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="honorEventArgs">荣誉变更事件参数</param>
    internal HonorEventArgs(Guid serviceId, Guid connectionId, string eventName, OnebotHonorEventArgs honorEventArgs)
        : base(serviceId, connectionId, eventName, honorEventArgs.SelfId, honorEventArgs.Time, SourceFlag.Group)
    {
        TargetUser  = new User(serviceId, connectionId, honorEventArgs.UserId);
        SourceGroup = new Group(connectionId, honorEventArgs.GroupId);
        Honor       = honorEventArgs.HonorType;
    }

#endregion
}