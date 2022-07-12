using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Net.Config;

namespace Sora.Net.Records;

internal static class ServiceRecord
{
    /// <summary>
    /// 服务信息
    /// Key:服务标识符[Service Id]
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, ServiceConfig> _serviceConfigs = new();

    private static readonly HashSet<Guid> _deadService = new();

    /// <summary>
    /// 用户是否为超级管理员
    /// </summary>
    public static bool IsSuperUser(Guid service, long userId)
    {
        if (_deadService.Contains(service) || !_serviceConfigs.ContainsKey(service))
            return false;
        return _serviceConfigs[service].SuperUsers.Contains(userId);
    }

    /// <summary>
    /// 用户是否为屏蔽用户
    /// </summary>
    public static bool IsBlockUser(Guid service, long userId)
    {
        if (_deadService.Contains(service) || !_serviceConfigs.ContainsKey(service))
            return false;
        return _serviceConfigs[service].BlockUsers.Contains(userId);
    }

    #region flag

    public static bool IsEnableSoraCommandManager(Guid service)
    {
        if (_deadService.Contains(service) || !_serviceConfigs.ContainsKey(service))
            return false;
        return _serviceConfigs[service].EnableSoraCommandManager;
    }

    public static bool IsEnableSocketMessage(Guid service)
    {
        if (_deadService.Contains(service) || !_serviceConfigs.ContainsKey(service))
            return false;
        return _serviceConfigs[service].EnableSocketMessage;
    }

    public static bool IsAutoMarkMessageRead(Guid service)
    {
        if (_deadService.Contains(service) || !_serviceConfigs.ContainsKey(service))
            return false;
        return _serviceConfigs[service].AutoMarkMessageRead;
    }

    #endregion

    #region 基本操作

    public static bool AddOrUpdateRecord(Guid service, ServiceConfig config)
    {
        if (_serviceConfigs.ContainsKey(service))
        {
            if (_deadService.Contains(service))
                _deadService.Remove(service);
            ServiceConfig old = _serviceConfigs[service];
            return _serviceConfigs.TryUpdate(service, config, old);
        }

        return _serviceConfigs.TryAdd(service, config);
    }

    public static bool RemoveRecord(Guid service)
    {
        _deadService.Add(service);
        //防止多线程冲突
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            _deadService.Remove(service);
        });
        return _serviceConfigs.TryRemove(service, out _);
    }

    public static bool Exists(Guid service)
    {
        return _serviceConfigs.ContainsKey(service);
    }

    public static bool AddSuperUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service))
            return false;
        return _serviceConfigs[service].SuperUsers.Add(userId);
    }

    public static bool RemoveSuperUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service))
            return false;
        return _serviceConfigs[service].SuperUsers.Remove(userId);
    }

    public static bool AddBlockUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service))
            return false;
        return _serviceConfigs[service].BlockUsers.Add(userId);
    }

    public static bool RemoveBlockUser(Guid service, long userId)
    {
        if (!_serviceConfigs.ContainsKey(service))
            return false;
        return _serviceConfigs[service].BlockUsers.Remove(userId);
    }

    #endregion
}