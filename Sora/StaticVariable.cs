using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Newtonsoft.Json.Linq;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Net.Config;
using YukariToolBox.LightLog;

namespace Sora;

/// <summary>
/// 静态变量存放区
/// </summary>
public static class StaticVariable
{
    /// <summary>
    /// 连续对话匹配上下文
    /// Key:当前对话标识符[Session Id]
    /// </summary>
    internal static readonly ConcurrentDictionary<Guid, WaitingInfo> WaitingDict = new();

    /// <summary>
    /// WS静态连接记录表
    /// Key:链接标识符[Conn Id]
    /// </summary>
    internal static readonly ConcurrentDictionary<Guid, SoraConnectionInfo> ConnectionInfos = new();

    /// <summary>
    /// 服务信息
    /// Key:服务标识符[Service Id]
    /// </summary>
    internal static readonly ConcurrentDictionary<Guid, ServiceConfig> ServiceConfigs = new();

    /// <summary>
    /// 版本号
    /// </summary>
    public const string VERSION = "1.0.0-rc65";

    /// <summary>
    /// Onebot版本
    /// </summary>
    public const string ONEBOT_PROTOCOL = "11";

    /// <summary>
    /// 清除服务数据
    /// </summary>
    /// <param name="serviceId">服务标识</param>
    internal static void DisposeService(Guid serviceId)
    {
        Log.Debug("Sora", "Detect service dispose, cleanup service config...");

        //清空等待信息
        List<KeyValuePair<Guid, WaitingInfo>> removeWaitList =
            WaitingDict.Where(i => i.Value.ServiceId == serviceId)
                       .ToList();
        foreach ((Guid guid, WaitingInfo waitingInfo) in removeWaitList)
        {
            waitingInfo.Semaphore.Set();
            WaitingDict.TryRemove(guid, out _);
        }

        //清空服务信息
        ServiceConfigs.TryRemove(serviceId, out _);

        Log.Debug("Sora", "Service config cleanup finished");
    }
}