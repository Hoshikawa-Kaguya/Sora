using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sora.Entities.Base;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Interfaces;
using YukariToolBox.LightLog;

namespace Sora.Net.Records;

internal static class ConnectionRecord
{
    /// <summary>
    /// WS静态连接记录表
    /// Key:链接标识符[Conn Id]
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, SoraConnectionInfo> _connections = new();

    private static readonly HashSet<Guid> _deadConn = new();

#region 连接管理

    /// <summary>
    /// 添加服务器连接记录
    /// </summary>
    /// <param name="serviceId">服务Id</param>
    /// <param name="connId">连接标识</param>
    /// <param name="socket">连接信息</param>
    /// <param name="apiTimeout">api超时</param>
    public static bool AddNewConn(Guid serviceId, Guid connId, ISoraSocket socket, TimeSpan apiTimeout)
    {
        //检查是否已存在值
        if (_connections.ContainsKey(connId))
            return false;
        if (_deadConn.Contains(connId))
            _deadConn.Remove(connId);
        //selfId均在第一次链接开启时留空，并在meta事件触发后更新
        return _connections.TryAdd(connId, new SoraConnectionInfo(serviceId, connId, socket, DateTime.Now, apiTimeout));
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public static void CloseConn(Guid connId)
    {
        if (!_connections.ContainsKey(connId))
        {
            Log.Error("Socket", $"连接不可用[{connId}]1");
            return;
        }

        //关闭链接并标记
        _deadConn.Add(connId);
        bool closeFailed = false;
        try
        {
            _connections[connId].Connection.Close();
        }
        catch (Exception e)
        {
            Log.Error(e, "Socket", "无法关闭socket连接");
            closeFailed = true;
        }

        //防止多线程冲突
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            _deadConn.Remove(connId);
        });
        if (!_connections.TryRemove(connId, out _) || closeFailed)
            Log.Error("Socket", "关闭socket连接时发生错误");
    }

    public static List<SoraConnectionInfo> GetConnList(Guid serviceId)
    {
        return _connections.Where(c => c.Value.ApiInstance.ServiceId == serviceId && !_deadConn.Contains(c.Key))
                           .Select(c => c.Value).ToList();
    }

    public static Dictionary<Guid, SoraConnectionInfo> GetTimeoutConn(Guid     serviceId,
                                                                      DateTime now,
                                                                      TimeSpan heartbeatTimeout)
    {
        return _connections
               .Where(conn => conn.Value.ApiInstance.ServiceId == serviceId
                              && now - conn.Value.LastHeartBeatTime > heartbeatTimeout)
               .ToDictionary(conn => conn.Key, conn => conn.Value);
    }

    public static bool GetConn(Guid connId, out SoraConnectionInfo connection)
    {
        if (!_connections.ContainsKey(connId) || _deadConn.Contains(connId))
        {
            Log.Error("Socket", $"连接不可用[{connId}]2");
            connection = default;
            return false;
        }

        connection = _connections[connId];
        return true;
    }

    public static bool Exists(Guid connId)
    {
        return _connections.ContainsKey(connId) && !_deadConn.Contains(connId);
    }

    public static bool IsEmpty()
    {
        return _connections.IsEmpty;
    }

    public static int ConnCount()
    {
        return _connections.Count;
    }

#endregion

#region 连接参数

    /// <summary>
    /// 获取当前登录连接的账号ID
    /// </summary>
    /// <param name="connId">连接标识</param>
    /// <param name="userId">UID</param>
    public static bool GetLoginUid(Guid connId, out long userId)
    {
        if (_connections.TryGetValue(connId, out SoraConnectionInfo connection))
        {
            userId = connection.LoginUid;
            return true;
        }

        userId = -1;
        return false;
    }

    /// <summary>
    /// 刷新心跳包记录
    /// </summary>
    /// <param name="connId">连接标识</param>
    /// <param name="uid">新的UID</param>
    public static void UpdateLoginUid(Guid connId, long uid)
    {
        if (!_connections.ContainsKey(connId) || _deadConn.Contains(connId))
            return;
        SoraConnectionInfo oldInfo = _connections[connId];
        SoraConnectionInfo newInfo = oldInfo;
        newInfo.LoginUid = uid;
        _connections.TryUpdate(connId, newInfo, oldInfo);
    }

    /// <summary>
    /// 刷新心跳包记录
    /// </summary>
    /// <param name="connId">连接标识</param>
    public static void UpdateHeartBeat(Guid connId)
    {
        if (!_connections.ContainsKey(connId) || _deadConn.Contains(connId))
            return;
        SoraConnectionInfo oldInfo = _connections[connId];
        SoraConnectionInfo newInfo = oldInfo;
        newInfo.LastHeartBeatTime = DateTime.Now;
        _connections.TryUpdate(connId, newInfo, oldInfo);
    }

    /// <summary>
    /// 获取当前连接设置的API超时
    /// </summary>
    /// <param name="connId">连接标识</param>
    /// <param name="timeout">超时</param>
    public static bool GetApiTimeout(Guid connId, out TimeSpan timeout)
    {
        if (_connections.TryGetValue(connId, out SoraConnectionInfo connection))
        {
            timeout = connection.ApiTimeout;
            return true;
        }

        timeout = TimeSpan.Zero;
        return false;
    }

#endregion

#region API

    public static SoraApi GetApi(Guid connId)
    {
        if (!_connections.ContainsKey(connId) || _deadConn.Contains(connId))
        {
            Log.Error("Socket", $"连接不可用[{connId}]3");
            return null;
        }

        return _connections[connId].ApiInstance;
    }

    public static SoraApi GetApi(long uid)
    {
        if (_connections.Values.Any(conn => conn.LoginUid == uid))
            return _connections.Values.Where(conn => conn.LoginUid == uid).Select(conn => conn.ApiInstance).First();

        return null;
    }

#endregion
}