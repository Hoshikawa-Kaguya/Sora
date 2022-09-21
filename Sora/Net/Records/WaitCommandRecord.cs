using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sora.Attributes;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

namespace Sora.Net.Records;

internal static class WaitCommandRecord
{
    /// <summary>
    /// 连续对话匹配上下文
    /// Key:当前对话标识符[Session Id]
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, WaitingInfo> _waitingDict = new();

    private static readonly HashSet<Guid> _deadSession = new();

    /// <summary>
    /// 等待下一个
    /// </summary>
    public static bool WaitForNext(this WaitingInfo waitInfo, TimeSpan? timeout, out object eventArgs)
    {
        Guid sessionId = Guid.NewGuid();
        //添加上下文并等待信号量
        _waitingDict.TryAdd(sessionId, waitInfo);
        //是否正常接受到触发信号
        bool receiveSignal =
            //等待信号量
            _waitingDict[sessionId].Semaphore.WaitOne(timeout ?? TimeSpan.FromMilliseconds(-1));
        //检查是否为已被销毁的一次对话
        if (_deadSession.Contains(sessionId))
        {
            _deadSession.Remove(sessionId);
            eventArgs = null;
            return false;
        }

        //取出匹配指令的事件参数并删除上一次的上下文
        object retEventArgs = receiveSignal ? _waitingDict[sessionId].EventArgs : null;
        _waitingDict.TryRemove(sessionId, out _);
        eventArgs = retEventArgs;
        return receiveSignal;
    }

    /// <summary>
    /// 判断同一源
    /// </summary>
    public static bool GetSameSource(this WaitingInfo waitInfo)
    {
        return _waitingDict.Any(i => i.Value.IsSameSource(waitInfo));
    }

    /// <summary>
    /// 清空等待信息
    /// </summary>
    public static void DisposeSession(Guid serviceId)
    {
        List<KeyValuePair<Guid, WaitingInfo>> removeWaitList =
            _waitingDict.Where(i => i.Value.ServiceId == serviceId).ToList();
        foreach ((Guid guid, WaitingInfo waitingInfo) in removeWaitList)
        {
            _deadSession.Add(guid);
            waitingInfo.Semaphore.Set();
            _waitingDict.TryRemove(guid, out _);
        }
    }

    /// <summary>
    /// 获取匹配的等待指令
    /// </summary>
    public static List<Guid> GetMatchCommand(BaseMessageEventArgs eventArgs)
    {
        return _waitingDict.Where(command => WaitingCommandMatch(command.Value, eventArgs)).Select(i => i.Key).ToList();
    }

    /// <summary>
    /// 更新指令的eventarg
    /// </summary>
    public static void UpdateRecord(Guid sessionId, object eventArgs)
    {
        //更新等待列表，设置为当前的eventArgs
        WaitingInfo oldInfo = _waitingDict[sessionId];
        WaitingInfo newInfo = oldInfo;
        newInfo.EventArgs = eventArgs;
        _waitingDict.TryUpdate(sessionId, newInfo, oldInfo);
        _waitingDict[sessionId].Semaphore.Set();
    }

#region 等待指令匹配

    [NeedReview("ALL")]
    private static bool WaitingCommandMatch(WaitingInfo command, BaseMessageEventArgs eventArgs)
    {
        switch (eventArgs.SourceType)
        {
            case SourceFlag.Group:
            {
                bool preMatch =
                    //判断发起源
                    command.SourceFlag == SourceFlag.Group
                    //判断来自同一个连接
                    && command.ConnectionId == eventArgs.SoraApi.ConnectionId
                    //判断来着同一个群
                    && command.Source.g == (eventArgs as GroupMessageEventArgs)?.SourceGroup
                    //判断来自同一人
                    && command.Source.u == eventArgs.Sender;
                if (!preMatch)
                    return false;
                break;
            }
            case SourceFlag.Private:
            {
                bool preMatch =
                    //判断发起源
                    command.SourceFlag == SourceFlag.Private
                    //判断来自同一个连接
                    && command.ConnectionId == eventArgs.SoraApi.ConnectionId
                    //判断来自同一人
                    && command.Source.u == eventArgs.Sender;
                if (!preMatch)
                    return false;
                break;
            }
            default:
                return false;
        }

        if (command.MatchFunc is not null)
            return command.MatchFunc(eventArgs);
        return command.CommandExpressions?.Any(regex => Regex.IsMatch(eventArgs.Message.RawText,
                                                                      regex,
                                                                      RegexOptions.Compiled | command.RegexOptions))
               ?? false;
    }

#endregion
}