using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sora.Converter;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.OnebotModel.ExtraEvent;
using Sora.Util;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 频道消息事件参数
/// </summary>
public class GuildMessageEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 消息内容
    /// </summary>
    public Message Messages { get; }

    /// <summary>
    /// 消息发送者实例
    /// </summary>
    public GuildUser Sender { get; }

    /// <summary>
    /// 发送者信息
    /// </summary>
    public GuildSenderInfo SenderInfo { get; }

    /// <summary>
    /// 源频道
    /// </summary>
    public Guild SourceGuild { get; }

    /// <summary>
    /// 源子频道
    /// </summary>
    public Channel SourceChannel { get; }

    /// <summary>
    /// 登陆账号的频道ID
    /// </summary>
    public long SelfGuildId { get; }

    #endregion

    #region 构造方法

    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">链接ID</param>
    /// <param name="eventName">事件名</param>
    /// <param name="guildMsgArgs">原始事件参数</param>
    internal GuildMessageEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                   GocqGuildMessageEventArgs guildMsgArgs) :
        base(serviceId, connectionId, eventName, guildMsgArgs.SelfID, guildMsgArgs.Time,
             new EventSource
             {
                 GuildId     = guildMsgArgs.GuildId,
                 ChannelId   = guildMsgArgs.ChannelId,
                 UserGuildId = guildMsgArgs.UserId
             })
    {
        Messages = new Message(serviceId, connectionId, guildMsgArgs.MessageId, string.Empty,
                              guildMsgArgs.MessageList.ToMessageBody(), guildMsgArgs.Time, 0, null);
        Messages.RawText = Messages.MessageBody.Serialize();
        Sender           = new GuildUser(serviceId, connectionId, guildMsgArgs.UserId);
        SenderInfo       = guildMsgArgs.SenderInfo;
        SourceGuild      = new Guild(serviceId, connectionId, guildMsgArgs.GuildId);
        SourceChannel    = new Channel(serviceId, connectionId, guildMsgArgs.ChannelId, guildMsgArgs.GuildId);
        SelfGuildId      = guildMsgArgs.SelfTinyId;
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
    public async ValueTask<(ApiStatus apiStatus, string messageId)> Reply(MessageBody message,
                                                                          TimeSpan? timeout = null)
    {
        return await SoraApi.SendGuildMessage(SourceGuild.GuildId, SourceChannel.ChannelId, message, timeout);
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
    public ValueTask<GuildMessageEventArgs> WaitForNextMessageAsync(string[] commandExps, MatchType matchType,
                                                                    RegexOptions regexOptions = RegexOptions.None)
    {
        if (StaticVariable.ServiceInfos[SoraApi.ServiceId].EnableSoraCommandManager)
            return ValueTask.FromResult(WaitForNextMessage(commandExps, matchType, SourceFlag.Guild,
                                                           regexOptions, null, null) as GuildMessageEventArgs);
        Helper.CommandDisableTip();
        return ValueTask.FromResult<GuildMessageEventArgs>(null);
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
    public ValueTask<GuildMessageEventArgs> WaitForNextMessageAsync(string[] commandExps, MatchType matchType,
                                                                    TimeSpan timeout,
                                                                    Func<ValueTask> timeoutTask = null,
                                                                    RegexOptions regexOptions = RegexOptions.None)
    {
        if (StaticVariable.ServiceInfos[SoraApi.ServiceId].EnableSoraCommandManager)
            return ValueTask.FromResult(WaitForNextMessage(commandExps, matchType, SourceFlag.Guild,
                                                           regexOptions, timeout, timeoutTask) as GuildMessageEventArgs);
        Helper.CommandDisableTip();
        return ValueTask.FromResult<GuildMessageEventArgs>(null);
    }

    /// <summary>
    /// 等待下一条消息触发
    /// </summary>
    /// <param name="commandExp">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <returns>触发后的事件参数</returns>
    public ValueTask<GuildMessageEventArgs> WaitForNextMessageAsync(string commandExp, MatchType matchType,
                                                                    RegexOptions regexOptions = RegexOptions.None)
    {
        if (StaticVariable.ServiceInfos[SoraApi.ServiceId].EnableSoraCommandManager)
            return WaitForNextMessageAsync(new[] {commandExp}, matchType, regexOptions);
        Helper.CommandDisableTip();
        return ValueTask.FromResult<GuildMessageEventArgs>(null);
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
    public ValueTask<GuildMessageEventArgs> WaitForNextMessageAsync(string commandExp, MatchType matchType,
                                                                    TimeSpan timeout,
                                                                    Func<ValueTask> timeoutTask = null,
                                                                    RegexOptions regexOptions = RegexOptions.None)
    {
        if (StaticVariable.ServiceInfos[SoraApi.ServiceId].EnableSoraCommandManager)
            return WaitForNextMessageAsync(new[] {commandExp}, matchType, timeout, timeoutTask, regexOptions);
        Helper.CommandDisableTip();
        return ValueTask.FromResult<GuildMessageEventArgs>(null);
    }

    #endregion
}