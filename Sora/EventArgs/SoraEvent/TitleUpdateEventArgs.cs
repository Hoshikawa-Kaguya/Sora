using System;
using Sora.Entities;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 群成员头衔更新事件
/// </summary>
public sealed class TitleUpdateEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 运气王
    /// </summary>
    public User TargetUser { get; private set; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public string NewTitle { get; private set; }

    #endregion

    internal TitleUpdateEventArgs(Guid    serviceId, Guid connectionId, string eventName,
        OnebotMemberTitleUpdatedEventArgs eventArgs) :
        base(serviceId, connectionId, eventName, eventArgs.SelfId, eventArgs.Time)
    {
        TargetUser = new User(serviceId, connectionId, eventArgs.UserId);
        NewTitle   = eventArgs.NewTitle;
    }
}