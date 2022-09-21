using System;
using Sora.Entities.Base;
using Sora.Enumeration;
using Sora.Net.Records;
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
    public SoraApi SoraApi { get; }

    /// <summary>
    /// 链接ID
    /// </summary>
    public Guid ConnId { get; }

    /// <summary>
    /// 服务ID
    /// </summary>
    public Guid ServiceId { get; }

    /// <summary>
    /// 当前事件名
    /// </summary>
    public string EventName { get; }

    /// <summary>
    /// 事件产生时间
    /// </summary>
    public DateTime Time { get; }

    /// <summary>
    /// 接收当前事件的机器人UID
    /// </summary>
    public long LoginUid { get; }

    /// <summary>
    /// 事件产生时间戳
    /// </summary>
    internal long TimeStamp { get; set; }

    /// <summary>
    /// <para>是否在处理本次事件后再次触发其他事件，默认为触发[<see langword="true"/>]</para>
    /// <para>如:处理Command后可以将此值设置为<see langword="false"/>来阻止后续的事件触发，为<see langword="true"/>时则会触发其他相匹配的指令和事件</para>
    /// <para>如果出现了不同表达式同时被触发且优先级相同的情况，则这几个指令的执行顺序将是不确定的，请避免这种情况的发生</para>
    /// </summary>
    public bool IsContinueEventChain { get; set; }

    /// <summary>
    /// 消息来源类型
    /// </summary>
    public SourceFlag SourceType { get; }

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
    /// <param name="sourceType">来源</param>
    internal BaseSoraEventArgs(Guid       serviceId,
                               Guid       connectionId,
                               string     eventName,
                               long       loginUid,
                               long       time,
                               SourceFlag sourceType)
    {
        SoraApi              = ConnectionRecord.GetApi(connectionId);
        ServiceId            = serviceId;
        ConnId               = connectionId;
        EventName            = eventName;
        LoginUid             = loginUid;
        TimeStamp            = time;
        Time                 = time.ToDateTime();
        IsContinueEventChain = true;
        SourceType           = sourceType;
    }

#endregion
}