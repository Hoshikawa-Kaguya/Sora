using System;
using Sora.Entities;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 群戳一戳事件参数
/// </summary>
public sealed class GroupPokeEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 发送者
    /// </summary>
    public User SendUser { get; private set; }

    /// <summary>
    /// 被戳者
    /// </summary>
    public User TargetUser { get; private set; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public Group SourceGroup { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="pokeEventArgs">戳一戳事件参数</param>
    internal GroupPokeEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                OnebotPokeOrLuckyEventArgs pokeEventArgs) :
        base(serviceId, connectionId, eventName, pokeEventArgs.SelfID, pokeEventArgs.Time)
    {
        SendUser    = new User(serviceId, connectionId, pokeEventArgs.UserId);
        TargetUser  = new User(serviceId, connectionId, pokeEventArgs.TargetId);
        SourceGroup = new Group(serviceId, connectionId, pokeEventArgs.GroupId);
    }

    #endregion
}