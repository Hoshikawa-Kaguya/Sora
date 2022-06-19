using System;
using System.Linq;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// 是否为Bot账号所发送的消息
    /// </summary>
    public bool IsSelfMessage { get; }

    /// <summary>
    /// 是否为机器人管理员
    /// </summary>
    public bool IsSuperUser { get; }

    /// <summary>
    /// 在匹配到指令时则此值为匹配到的正则表达式
    /// </summary>
    public Regex[] CommandRegex { get; internal set; }

    /// <summary>
    /// 在匹配到动态指令时此值为匹配到的动态指令ID
    /// </summary>
    public Guid CommandId { get; internal set; }

    /// <summary>
    /// 在匹配到指令时此值为匹配到的指令名
    /// </summary>
    public string CommandName { get; internal set; }

    #endregion

    internal BaseMessageEventArgs(Guid                   serviceId, Guid       connectionId, string eventName,
                                  BaseObMessageEventArgs msg,       SourceFlag source) :
        base(serviceId, connectionId, eventName, msg.SelfId, msg.Time, source)
    {
        //将api消息段转换为sorasegment
        Message = new MessageContext(connectionId, msg.MessageId, msg.RawMessage,
            msg.MessageList.ToMessageBody(),
            msg.Time, msg.Font, null);
        Sender        = new User(serviceId, connectionId, msg.UserId);
        IsSelfMessage = msg.UserId == msg.SelfId;
        IsSuperUser = msg.UserId is not 0 or -1 &&
            StaticVariable.ServiceConfigs[serviceId].SuperUsers.Any(id => id == msg.UserId);
    }
}