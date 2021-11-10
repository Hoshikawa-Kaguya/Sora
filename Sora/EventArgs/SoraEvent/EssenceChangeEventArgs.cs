using System;
using Sora.Entities;
using Sora.Enumeration.EventParamsType;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 精华消息变动事件参数
/// </summary>
public class EssenceChangeEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 消息ID
    /// </summary>
    public long MessageId { get; internal set; }

    /// <summary>
    /// 精华设置者
    /// </summary>
    public User Operator { get; internal set; }

    /// <summary>
    /// 消息发送者
    /// </summary>
    public User Sender { get; internal set; }

    /// <summary>
    /// 消息发送者
    /// </summary>
    public Group SourceGroup { get; internal set; }

    /// <summary>
    /// 精华变动类型
    /// </summary>
    public EssenceChangeType EssenceChangeType { get; internal set; }

    #endregion

    #region 构造函数

    internal EssenceChangeEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                    OnebotEssenceChangeEventArgs essenceChangeEvent) :
        base(serviceId, connectionId, eventName, essenceChangeEvent.SelfID, essenceChangeEvent.Time)
    {
        MessageId         = essenceChangeEvent.MessageId;
        Operator          = new User(serviceId, connectionId, essenceChangeEvent.OperatorId);
        Sender            = new User(serviceId, connectionId, essenceChangeEvent.SenderId);
        SourceGroup       = new Group(serviceId, connectionId, essenceChangeEvent.GroupId);
        EssenceChangeType = essenceChangeEvent.EssenceChangeType;
    }

    #endregion
}