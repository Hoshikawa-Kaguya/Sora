using System;
using Sora.Entities;
using Sora.Enumeration;
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
    public User TargetUser { get; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public string NewTitle { get; }

#endregion

    internal TitleUpdateEventArgs(Guid                              serviceId,
                                  Guid                              connectionId,
                                  string                            eventName,
                                  OnebotMemberTitleUpdatedEventArgs eventArgs)
        : base(serviceId, connectionId, eventName, eventArgs.SelfId, eventArgs.Time, SourceFlag.Group)
    {
        TargetUser = new User(serviceId, connectionId, eventArgs.UserId);
        NewTitle   = eventArgs.NewTitle;
    }
}