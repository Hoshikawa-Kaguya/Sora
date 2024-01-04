using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sora.Command;
using Sora.Converter;
using Sora.Entities;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.Net.Records;
using Sora.OnebotModel.OnebotEvent.MessageEvent;
using Sora.Serializer;

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

    internal BaseMessageEventArgs(Guid                   serviceId,
                                  Guid                   connectionId,
                                  string                 eventName,
                                  BaseObMessageEventArgs msg,
                                  SourceFlag             source)
        : base(serviceId, connectionId, eventName, msg.SelfId, msg.Time, source)
    {
        //将api消息段转换为sorasegment
        Message = new MessageContext(connectionId,
                                     msg.MessageId,
                                     msg.RawMessage,
                                     msg.MessageList.ToMessageBody(),
                                     msg.Time,
                                     msg.Font,
                                     null);
        
        //空raw信息兼容
        if (string.IsNullOrEmpty(msg.RawMessage))
        {
            Message.RawText = Message.MessageBody.SerializeToCq();
        }
        Sender = new User(serviceId, connectionId, msg.UserId);
        IsSelfMessage = msg.UserId == msg.SelfId;
        IsSuperUser   = msg.UserId is not (0 or -1) && ServiceRecord.IsSuperUser(serviceId, msg.UserId);
    }

    /// <summary>
    /// 快速回复
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="timeout">覆盖原有超时</param>
    /// <returns>
    /// <para><see cref="ApiStatusType"/> API执行状态</para>
    /// <para><see langword="messageId"/> 发送消息的id</para>
    /// </returns>
    public virtual async ValueTask<(ApiStatus apiStatus, int messageId)> Reply(
        MessageBody message,
        TimeSpan?   timeout = null)
    {
        return this switch
               {
                   GroupMessageEventArgs groupMessage     => await groupMessage.Reply(message, timeout),
                   PrivateMessageEventArgs privateMessage => await privateMessage.Reply(message, timeout),
                   _                                      => throw new ArgumentOutOfRangeException()
               };
    }

#region 连续指令

    /// <summary>
    /// <para>等待下一条消息触发正则表达式</para>
    /// <para>当所在的上下文被重复触发时则会直接返回<see langword="null"/></para>
    /// </summary>
    internal object WaitForNextRegexMessage(long            sourceUid,
                                            string[]        commandExps,
                                            MatchType       matchType,
                                            RegexOptions    regexOptions,
                                            TimeSpan?       timeout,
                                            Func<ValueTask> timeoutTask,
                                            long            sourceGroup = 0)
    {
        //生成指令上下文
        WaitingInfo waitInfo = CommandUtils.GenerateWaitingCommandInfo(sourceUid,
                                                                       sourceGroup,
                                                                       commandExps,
                                                                       matchType,
                                                                       SourceType,
                                                                       regexOptions,
                                                                       ConnId,
                                                                       ServiceId);
        return WaitForNextMessage(waitInfo, timeout, timeoutTask);
    }

    /// <summary>
    /// 等待下一条消息触发自定义匹配方法
    /// </summary>
    internal object WaitForNextCustomMessage(long                             sourceUid,
                                             Func<BaseMessageEventArgs, bool> matchFunc,
                                             TimeSpan?                        timeout,
                                             Func<ValueTask>                  timeoutTask,
                                             long                             sourceGroup = 0)
    {
        //生成指令上下文
        WaitingInfo waitInfo =
            CommandUtils.GenerateWaitingCommandInfo(sourceUid, sourceGroup, matchFunc, SourceType, ConnId, ServiceId);
        return WaitForNextMessage(waitInfo, timeout, timeoutTask);
    }

    /// <summary>
    /// <para>等待下一条消息触发</para>
    /// <para>当所在的上下文被重复触发时则会直接返回<see langword="null"/></para>
    /// </summary>
    internal object WaitForNextMessage(WaitingInfo waitInfo, TimeSpan? timeout, Func<ValueTask> timeoutTask)
    {
        //检查是否为初始指令重复触发
        if (waitInfo.GetSameSource())
            return null;
        //连续指令不再触发后续事件
        IsContinueEventChain = false;
        //在超时时执行超时任务
        if (!waitInfo.WaitForNext(timeout, out object e) && timeoutTask != null)
            Task.Run(timeoutTask.Invoke);
        return e;
    }

#endregion
}