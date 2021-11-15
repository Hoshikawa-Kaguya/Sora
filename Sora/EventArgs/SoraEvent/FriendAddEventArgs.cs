using System;
using Sora.Entities;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 好友添加事件参数
/// </summary>
public sealed class FriendAddEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 新好友
    /// </summary>
    public User NewFriend { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="friendAddArgs">好友添加事件参数</param>
    internal FriendAddEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                OnebotFriendAddEventArgs friendAddArgs) :
        base(serviceId, connectionId, eventName, friendAddArgs.SelfID, friendAddArgs.Time)
    {
        NewFriend = new User(serviceId, connectionId, friendAddArgs.UserId);
    }

    #endregion
}