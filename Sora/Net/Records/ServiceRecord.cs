using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Interfaces;
using Sora.Net.Config;
using YukariToolBox.LightLog;

namespace Sora.Net.Records;

internal static class ServiceRecord
{
    /// <summary>
    /// 服务信息
    /// Key:服务标识符[Service Id]
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, (ServiceConfig c, ISoraService s)> _servicesDict = new();

    private static readonly HashSet<Guid> _deadService = new();

    /// <summary>
    /// 用户是否为超级管理员
    /// </summary>
    public static bool IsSuperUser(Guid service, long userId)
    {
        if (!ServiceAlive("SuperUser", service))
            return false;
        return _servicesDict[service].c.SuperUsers.Contains(userId);
    }

    /// <summary>
    /// 用户是否为屏蔽用户
    /// </summary>
    public static bool IsBlockUser(Guid service, long userId)
    {
        if (!ServiceAlive("BlockUser", service))
            return false;
        return _servicesDict[service].c.BlockUsers.Contains(userId);
    }

#region flag

    public static bool IsEnableSoraCommandManager(Guid service)
    {
        if (!ServiceAlive("CommandManager", service))
            return false;
        return _servicesDict[service].c.EnableSoraCommandManager;
    }

    public static bool IsEnableSocketMessage(Guid service)
    {
        if (!ServiceAlive("SocketMessage", service))
            return false;
        return _servicesDict[service].c.EnableSocketMessage;
    }

    public static bool IsAutoMarkMessageRead(Guid service)
    {
        if (!ServiceAlive("AutoMarkMessage", service))
            return false;
        return _servicesDict[service].c.AutoMarkMessageRead;
    }

#endregion

#region 基本操作

    public static bool AddOrUpdateRecord(Guid service, ServiceConfig config, ISoraService serviceInstance)
    {
        if (ServiceExists("SoraService", service, true))
        {
            if (_deadService.Contains(service))
                _deadService.Remove(service);
            (ServiceConfig c, ISoraService s) old = _servicesDict[service];
            return _servicesDict.TryUpdate(service, (config, serviceInstance), old);
        }

        return _servicesDict.TryAdd(service, (config, serviceInstance));
    }

    public static ISoraService GetService(Guid serviceId)
    {
        return _servicesDict[serviceId].s;
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
        return _servicesDict.TryRemove(service, out _);
    }

    public static bool Exists(Guid service)
    {
        return ServiceExists("SoraService", service, false);
    }

    public static bool AddSuperUser(Guid service, long userId)
    {
        if (!ServiceExists("SoraService", service, false))
            return false;
        return _servicesDict[service].c.SuperUsers.Add(userId);
    }

    public static bool RemoveSuperUser(Guid service, long userId)
    {
        if (!ServiceExists("SoraService", service, false))
            return false;
        return _servicesDict[service].c.SuperUsers.Remove(userId);
    }

    public static bool AddBlockUser(Guid service, long userId)
    {
        if (!ServiceExists("SoraService", service, false))
            return false;
        return _servicesDict[service].c.BlockUsers.Add(userId);
    }

    public static bool RemoveBlockUser(Guid service, long userId)
    {
        if (!ServiceExists("SoraService", service, false))
            return false;
        return _servicesDict[service].c.BlockUsers.Remove(userId);
    }

#endregion

#region 指令相关

    /// <summary>
    /// 是否为群组中禁用的指令
    /// </summary>
    public static bool IsGroupBlockedCommand(Guid service, long groupId, string seriesName)
    {
        if (!ServiceAlive("Command", service))
            return false;
        return _servicesDict[service].c.GroupBanCommand.TryGetValue(groupId, out HashSet<string> value)
               && value!.Contains(seriesName);
    }

    /// <summary>
    /// 启用群组中禁用的指令
    /// </summary>
    public static bool EnableGroupCommand(Guid service, string seriesName, long groupId)
    {
        if (!ServiceAlive("Command", service))
            return false;
        if (!_servicesDict[service].c.GroupBanCommand.ContainsKey(groupId)) return false;

        Log.Info("CommandAdapter", $"正在启用群[{groupId}]的指令组[{seriesName}]");
        return _servicesDict[service].c.GroupBanCommand[groupId].Remove(seriesName);
    }

    /// <summary>
    /// 禁用群组中的指令
    /// </summary>
    public static bool DisableGroupCommand(Guid service, string seriesName, long groupId)
    {
        if (!ServiceAlive("Command", service))
            return false;
        if (!_servicesDict[service].c.GroupBanCommand.ContainsKey(groupId))
            _servicesDict[service].c.GroupBanCommand[groupId] = new HashSet<string>();

        Log.Info("CommandAdapter", $"正在禁用群[{groupId}]的指令组[{seriesName}]");
        return _servicesDict[service].c.GroupBanCommand[groupId].Add(seriesName);
    }

#endregion

#region 小工具

    private static bool ServiceAlive(string desc, Guid service)
    {
        if (_deadService.Contains(service) || !_servicesDict.ContainsKey(service))
        {
            Log.Warning(desc, $"不可用的Sora服务[{service}]");
            return false;
        }

        return true;
    }

    private static bool ServiceExists(string desc, Guid service, bool ignore)
    {
        if (!_servicesDict.ContainsKey(service))
        {
            if (!ignore) Log.Warning(desc, $"不存在的Sora服务[{service}]");
            return false;
        }

        return true;
    }

#endregion
}