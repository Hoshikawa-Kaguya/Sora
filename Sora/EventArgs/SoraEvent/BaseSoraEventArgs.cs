using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sora.Command;
using Sora.Entities.Base;
using Sora.Enumeration;
using Sora.Util;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 框架事件基类
/// </summary>
public abstract class BaseSoraEventArgs : System.EventArgs
{
    #region 属性

    /// <summary>
    /// 当前事件的API执行实例
    /// </summary>
    public SoraApi SoraApi { get; private set; }

    /// <summary>
    /// 当前事件名
    /// </summary>
    public string EventName { get; private set; }

    /// <summary>
    /// 事件产生时间
    /// </summary>
    public DateTime Time { get; private set; }

    /// <summary>
    /// 接收当前事件的机器人UID
    /// </summary>
    public long LoginUid { get; private set; }

    /// <summary>
    /// 事件产生时间戳
    /// </summary>
    private long TimeStamp { get; set; }

    /// <summary>
    /// <para>是否在处理本次事件后再次触发其他事件，默认为触发</para>
    /// <para>如:处理Command后可以将此值设置为<see langword="false"/>来阻止后续的事件触发，为<see langword="true"/>时则会触发其他相匹配的指令和事件</para>
    /// <para>如果出现了不同表达式同时被触发且优先级相同的情况，则这几个指令的执行顺序将是不确定的，请避免这种情况的发生</para>
    /// </summary>
    public bool IsContinueEventChain { get; set; }

    /// <summary>
    /// 连续对话的ID
    /// </summary>
    private Guid SessionId { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">当前服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="loginUid">当前使用的QQ号</param>
    /// <param name="time">连接时间</param>
    internal BaseSoraEventArgs(Guid serviceId, Guid connectionId, string eventName, long loginUid, long time)
    {
        SoraApi              = new SoraApi(serviceId, connectionId);
        EventName            = eventName;
        LoginUid             = loginUid;
        TimeStamp            = time;
        Time                 = time.ToDateTime();
        IsContinueEventChain = true;
        SessionId            = Guid.Empty;
    }

    #endregion

    #region 连续指令

    /// <summary>
    /// 等待下一条消息触发
    /// </summary>
    internal object WaitForNextMessage(long sourceUid, string[] commandExps, MatchType matchType,
                                       SourceFlag sourceFlag, RegexOptions regexOptions,
                                       TimeSpan? timeout, Func<ValueTask> timeoutTask, long sourceGroup = 0)
    {
        //生成指令上下文
        var waitInfo =
            CommandManager.GenWaitingCommandInfo(sourceUid, sourceGroup, commandExps, matchType, sourceFlag,
                                                 regexOptions, SoraApi.ConnectionId, SoraApi.ServiceId);
        //检查是否为初始指令重复触发
        if (StaticVariable.WaitingDict.Any(i => i.Value.IsSameSource(waitInfo)))
            return null;
        //连续指令不再触发后续事件
        IsContinueEventChain = false;
        var sessionId = Guid.NewGuid();
        SessionId = sessionId;
        //添加上下文并等待信号量
        StaticVariable.WaitingDict.TryAdd(sessionId, waitInfo);
        var receiveSignal = //是否正常接受到触发信号
            //等待信号量
            StaticVariable.WaitingDict[sessionId].Semaphore.WaitOne(timeout ?? TimeSpan.FromMilliseconds(-1));
        //取出匹配指令的事件参数并删除上一次的上下文
        var retEventArgs = receiveSignal ? StaticVariable.WaitingDict[sessionId].EventArgs : null;
        StaticVariable.WaitingDict.TryRemove(sessionId, out _);
        //在超时时执行超时任务
        if (!receiveSignal && timeoutTask != null) Task.Run(timeoutTask.Invoke);
        return retEventArgs;
    }

    #endregion
}