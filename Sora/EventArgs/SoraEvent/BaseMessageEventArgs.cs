using System;
using Sora.Converter;
using Sora.Entities;
using Sora.Enumeration;
using Sora.OnebotModel.OnebotEvent.MessageEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 用于存储消息和发送者的基类
/// </summary>
public abstract class BaseMessageEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 消息内容
    /// </summary>
    public MessageContext Message { get; }

    /// <summary>
    /// 消息发送者实例
    /// </summary>
    public User Sender { get; }

    #endregion

    internal BaseMessageEventArgs(Guid                      serviceId, Guid connectionId, string eventName,
                                  OnebotPrivateMsgEventArgs msg) :
        base(serviceId, connectionId, eventName, msg.SelfId, msg.Time, SourceFlag.Private)
    {
        //将api消息段转换为sorasegment
        Message = new MessageContext(serviceId, connectionId, msg.MessageId, msg.RawMessage,
            msg.MessageList.ToMessageBody(),
            msg.Time, msg.Font, null);
        Sender = new User(serviceId, connectionId, msg.UserId);
    }

    internal BaseMessageEventArgs(Guid                    serviceId, Guid connectionId, string eventName,
                                  OnebotGroupMsgEventArgs msg) :
        base(serviceId, connectionId, eventName, msg.SelfId, msg.Time, SourceFlag.Group)
    {
        //将api消息段转换为sorasegment
        Message = new MessageContext(serviceId, connectionId, msg.MessageId, msg.RawMessage,
            msg.MessageList.ToMessageBody(),
            msg.Time, msg.Font, null);
        Sender = new User(serviceId, connectionId, msg.UserId);
    }
}