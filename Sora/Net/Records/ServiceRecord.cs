using System;
using System.Collections.Concurrent;
using Sora.Net.Config;

namespace Sora.Net.Records;

internal static class ServiceRecord
{
    /// <summary>
    /// 服务信息
    /// Key:服务标识符[Service Id]
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, ServiceConfig> _serviceConfigs = new();

    /// <summary>
    /// 用户是否为超级管理员
    /// </summary>
    public static bool IsSuperUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service)) return false;
        return _serviceConfigs[service].SuperUsers.Contains(userId);
    }

    /// <summary>
    /// 用户是否为屏蔽用户
    /// </summary>
    public static bool IsBlockUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service)) return false;
        return _serviceConfigs[service].BlockUsers.Contains(userId);
    }

    #region flag

    public static bool IsEnableSoraCommandManager(Guid service)
    {
        if (!_serviceConfigs.ContainsKey(service)) return false;
        return _serviceConfigs[service].EnableSoraCommandManager;
    }

    public static bool IsEnableSocketMessage(Guid service)
    {
        if (!_serviceConfigs.ContainsKey(service)) return false;
        return _serviceConfigs[service].EnableSocketMessage;
    }

    public static bool IsAutoMarkMessageRead(Guid service)
    {
        if (!_serviceConfigs.ContainsKey(service)) return false;
        return _serviceConfigs[service].AutoMarkMessageRead;
    }

    #endregion

    #region 基本操作

    public static bool AddOrUpdateRecord(Guid service, ServiceConfig config)
    {
        if (_serviceConfigs.ContainsKey(service))
        {
            ServiceConfig old = _serviceConfigs[service];
            return _serviceConfigs.TryUpdate(service, config, old);
        }
        return _serviceConfigs.TryAdd(service, config);
    }

    public static bool RemoveRecord(Guid service)
    {
        return _serviceConfigs.TryRemove(service, out _);
    }

    public static bool Exists(Guid service)
    {
        return _serviceConfigs.ContainsKey(service);
    }

    public static bool AddSuperUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service)) return false;
        return _serviceConfigs[service].SuperUsers.Add(userId);
    }

    public static bool RemoveSuperUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service)) return false;
        return _serviceConfigs[service].SuperUsers.Remove(userId);
    }

    public static bool AddBlockUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service)) return false;
        return _serviceConfigs[service].BlockUsers.Add(userId);
    }

    public static bool RemoveBlockUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service)) return false;
        return _serviceConfigs[service].BlockUsers.Remove(userId);
    }

    #endregion
}