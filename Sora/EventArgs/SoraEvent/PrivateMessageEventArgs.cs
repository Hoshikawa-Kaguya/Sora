using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sora.Converter;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.Enumeration.EventParamsType;
using Sora.OnebotModel.OnebotEvent.MessageEvent;
using YukariToolBox.LightLog;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 私聊消息事件参数
/// </summary>
public sealed class PrivateMessageEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 消息内容
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// 消息发送者实例
    /// </summary>
    public User Sender { get; }

    /// <summary>
    /// 发送者信息
    /// </summary>
    public PrivateSenderInfo SenderInfo { get; }

    /// <summary>
    /// 是否为临时会话
    /// </summary>
    public bool IsTemporaryMessage { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="privateMsgArgs">私聊消息事件参数</param>
    internal PrivateMessageEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                     OnebotPrivateMsgEventArgs privateMsgArgs)
        : base(serviceId, connectionId, eventName, privateMsgArgs.SelfID, privateMsgArgs.Time)
    {
        //将api消息段转换为sorasegment
        Message = new Message(serviceId, connectionId, privateMsgArgs.MessageId, privateMsgArgs.RawMessage,
                              privateMsgArgs.MessageList.ToMessageBody(),
                              privateMsgArgs.Time, privateMsgArgs.Font, null);
        Sender             = new User(serviceId, connectionId, privateMsgArgs.UserId);
        IsTemporaryMessage = privateMsgArgs.SenderInfo.GroupId != null;

        //检查服务管理员权限
        var privateSenderInfo = privateMsgArgs.SenderInfo;
        if (privateSenderInfo.UserId != 0 && StaticVariable.ServiceInfos[serviceId].SuperUsers
                                                           .Any(id => id == privateSenderInfo.UserId))
            privateSenderInfo.Role = MemberRoleType.SuperUser;
        SenderInfo = privateSenderInfo;
    }

    #endregion

    #region 快捷方法

    /// <summary>
    /// 快速回复
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="timeout">覆盖原有超时</param>
    /// <returns>
    /// <para><see cref="ApiStatusType"/> API执行状态</para>
    /// <para><see langword="messageId"/> 发送消息的id</para>
    /// </returns>
    public async ValueTask<(ApiStatus apiStatus, int messageId)> Reply(
        MessageBody message, TimeSpan? timeout = null)
    {
        return await SoraApi.SendPrivateMessage(Sender.Id, message, timeout);
    }

    /// <summary>
    /// 没什么用的复读功能
    /// </summary>
    /// <returns>
    /// <para><see cref="ApiStatusType"/> API执行状态</para>
    /// <para><see langword="messageId"/> 发送消息的id</para>
    /// </returns>
    public async ValueTask<(ApiStatus apiStatus, int messageId)> Repeat()
    {
        return await SoraApi.SendPrivateMessage(Sender.Id, Message.MessageBody);
    }

    #endregion

    #region 连续对话

    /// <summary>
    /// 等待下一条消息触发
    /// </summary>
    /// <param name="commandExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <returns>触发后的事件参数</returns>
    public ValueTask<PrivateMessageEventArgs> WaitForNextMessageAsync(string[] commandExps, MatchType matchType,
                                                                      RegexOptions regexOptions = RegexOptions.None)
    {
        if (StaticVariable.ServiceInfos[SoraApi.ServiceId].EnableSoraCommandManager)
            return ValueTask.FromResult(WaitForNextMessage(Sender, commandExps, matchType,
                                                           SourceFlag.Private, regexOptions,
                                                           null, null) as PrivateMessageEventArgs);
        CommandDisableTip();
        return ValueTask.FromResult<PrivateMessageEventArgs>(null);
    }

    /// <summary>
    /// 等待下一条消息触发
    /// </summary>
    /// <param name="commandExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="timeout">超时</param>
    /// <param name="timeoutTask">超时后执行的动作</param>
    /// <returns>触发后的事件参数，超时后为<see langword="null"/></returns>
    public ValueTask<PrivateMessageEventArgs> WaitForNextMessageAsync(string[] commandExps, MatchType matchType,
                                                                      TimeSpan timeout,
                                                                      Func<ValueTask> timeoutTask = null,
                                                                      RegexOptions regexOptions = RegexOptions.None)
    {
        if (StaticVariable.ServiceInfos[SoraApi.ServiceId].EnableSoraCommandManager)
            return ValueTask.FromResult(WaitForNextMessage(Sender, commandExps, matchType,
                                                           SourceFlag.Private, regexOptions,
                                                           timeout, timeoutTask) as PrivateMessageEventArgs);
        CommandDisableTip();
        return ValueTask.FromResult<PrivateMessageEventArgs>(null);
    }

    /// <summary>
    /// 等待下一条消息触发
    /// </summary>
    /// <param name="commandExp">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <returns>触发后的事件参数</returns>
    public ValueTask<PrivateMessageEventArgs> WaitForNextMessageAsync(string commandExp, MatchType matchType,
                                                                      RegexOptions regexOptions = RegexOptions.None)
    {
        if (StaticVariable.ServiceInfos[SoraApi.ServiceId].EnableSoraCommandManager)
            return WaitForNextMessageAsync(new[] {commandExp}, matchType, regexOptions);
        CommandDisableTip();
        return ValueTask.FromResult<PrivateMessageEventArgs>(null);
    }

    /// <summary>
    /// 等待下一条消息触发
    /// </summary>
    /// <param name="commandExp">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="timeout">超时</param>
    /// <param name="timeoutTask">超时后执行的动作</param>
    /// <returns>触发后的事件参数，超时后为<see langword="null"/></returns>
    public ValueTask<PrivateMessageEventArgs> WaitForNextMessageAsync(string commandExp, MatchType matchType,
                                                                      TimeSpan timeout,
                                                                      Func<ValueTask> timeoutTask = null,
                                                                      RegexOptions regexOptions = RegexOptions.None)
    {
        if (StaticVariable.ServiceInfos[SoraApi.ServiceId].EnableSoraCommandManager)
            return ValueTask.FromResult(WaitForNextMessage(Sender, new[] {commandExp},
                                                           matchType,
                                                           SourceFlag.Private, regexOptions,
                                                           timeout, timeoutTask) as PrivateMessageEventArgs);
        CommandDisableTip();
        return ValueTask.FromResult<PrivateMessageEventArgs>(null);
    }

    #endregion

    #region 私有方法

    private void CommandDisableTip()
    {
        Log.Error("非法操作", "指令服务已被禁用，无法执行连续对话操作");
    }

    #endregion
}