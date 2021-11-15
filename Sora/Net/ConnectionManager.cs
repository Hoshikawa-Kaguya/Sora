using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;
using Sora.EventArgs.WebsocketEvent;
using Sora.Interfaces;
using Websocket.Client;
using YukariToolBox.LightLog;

namespace Sora.Net;

/// <summary>
/// 服务器连接管理器
/// 管理服务器链接和心跳包
/// </summary>
public sealed class ConnectionManager : IDisposable
{
    #region 属性

    private TimeSpan HeartBeatTimeOut { get; }

    private Timer HeartBeatTimer { set; get; }

    #endregion

    #region 回调事件

    /// <summary>
    /// 服务器事件回调
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型</typeparam>
    /// <param name="connectionId">连接Id</param>
    /// <param name="eventArgs">事件参数</param>
    public delegate ValueTask ServerAsyncCallBackHandler<in TEventArgs>(
        Guid connectionId, TEventArgs eventArgs) where TEventArgs : System.EventArgs;

    /// <summary>
    /// 打开连接回调
    /// </summary>
    public event ServerAsyncCallBackHandler<ConnectionEventArgs> OnOpenConnectionAsync;

    /// <summary>
    /// 关闭连接回调
    /// </summary>
    public event ServerAsyncCallBackHandler<ConnectionEventArgs> OnCloseConnectionAsync;

    #endregion

    #region 构造函数

    internal ConnectionManager(ISoraConfig config)
    {
        HeartBeatTimeOut = config.HeartBeatTimeOut;
    }

    #endregion

    #region 服务器连接管理

    /// <summary>
    /// 添加服务器连接记录
    /// </summary>
    /// <param name="serviceId">服务Id</param>
    /// <param name="connectionId">连接标识</param>
    /// <param name="socket">连接信息</param>
    /// <param name="selfId">机器人UID</param>
    /// <param name="apiTimeout">api超时</param>
    private bool AddConnection(Guid serviceId, Guid connectionId, ISoraSocket socket,
                               string selfId, TimeSpan apiTimeout)
    {
        //检查是否已存在值
        if (StaticVariable.ConnectionInfos.ContainsKey(connectionId)) return false;
        long.TryParse(selfId, out var uid);
        return StaticVariable.ConnectionInfos.TryAdd(connectionId, new SoraConnectionInfo
                                                         (serviceId, connectionId,
                                                          socket,
                                                          DateTime.Now,
                                                          uid, apiTimeout
                                                         ));
    }

    /// <summary>
    /// 移除服务器连接记录
    /// </summary>
    /// <param name="connectionId">连接标识</param>
    private bool RemoveConnection(Guid connectionId)
    {
        return StaticVariable.ConnectionInfos.TryRemove(connectionId, out _);
    }

    /// <summary>
    /// 检查是否存在连接
    /// </summary>
    /// <param name="connectionId">连接标识</param>
    internal bool ConnectionExists(Guid connectionId)
    {
        return StaticVariable.ConnectionInfos.ContainsKey(connectionId);
    }

    #endregion

    #region 服务器信息发送

    internal static bool SendMessage(Guid connectionId, string message)
    {
        if (StaticVariable.ConnectionInfos.All(connection => connection.Key != connectionId))
            return false;

        try
        {
            StaticVariable.ConnectionInfos[connectionId].Connection.Send(message);
            return true;
        }
        catch (Exception e)
        {
            Log.Error("ConnectionManager", $"Send message to client error\r\n{Log.ErrorLogBuilder(e)}");
            return false;
        }
    }

    #endregion

    #region 心跳包管理

    /// <summary>
    /// 启动心跳计时器
    /// </summary>
    internal void StartTimer(Guid serviceId)
    {
        HeartBeatTimer ??= new Timer(HeartBeatCheck, serviceId, HeartBeatTimeOut, HeartBeatTimeOut);
    }

    /// <summary>
    /// 心跳包超时检查
    /// </summary>
    internal void HeartBeatCheck(object serviceIdObj)
    {
        if (StaticVariable.ConnectionInfos.IsEmpty) return;
        var serviceId = (Guid) serviceIdObj;
        Log.Debug("HeartBeatCheck", $"service id={serviceId}");

        //查找超时连接
        var timeoutDict =
            StaticVariable.ConnectionInfos
                          .Where(conn => conn.Value.ServiceId                        == serviceId &&
                                         DateTime.Now - conn.Value.LastHeartBeatTime > HeartBeatTimeOut)
                          .ToDictionary(conn => conn.Key,
                                        conn => conn.Value);
        if (timeoutDict.Count == 0) return;
        Log.Warning("HeartBeatCheck", $"timeout connection count {timeoutDict.Count}");

        var needReconnect = new List<WebsocketClient>();
        //遍历超时的连接
        foreach (var (connection, info) in timeoutDict)
        {
            CloseConnection("Universal", info.SelfId, connection);
            Log.Error("HeartBeatCheck", $"Socket:[{connection}]connection time out，disconnect");
            //客户端尝试重连
            if (info.Connection.SocketType == SoraSocketType.Client &&
                info.Connection.SocketInstance is WebsocketClient c)
                needReconnect.Add(c);
        }

        if (needReconnect.Count != 0)
            needReconnect.ForEach(conn => conn.Reconnect());
    }

    /// <summary>
    /// 刷新心跳包记录
    /// </summary>
    /// <param name="connectionGuid">连接标识</param>
    internal static void HeartBeatUpdate(Guid connectionGuid)
    {
        var oldInfo = StaticVariable.ConnectionInfos[connectionGuid];
        var newInfo = oldInfo;
        newInfo.LastHeartBeatTime = DateTime.Now;
        StaticVariable.ConnectionInfos.TryUpdate(connectionGuid, newInfo, oldInfo);
    }

    #endregion

    #region 服务器事件

    /// <summary>
    /// 服务器链接开启事件
    /// </summary>
    /// <param name="role">通道标识</param>
    /// <param name="selfId">事件源</param>
    /// <param name="socket">连接</param>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connId">连接ID</param>
    /// <param name="apiTimeout">api超时</param>
    internal void OpenConnection(string role, string selfId, ISoraSocket socket, Guid serviceId, Guid connId,
                                 TimeSpan apiTimeout)
    {
        //添加服务器记录
        if (!AddConnection(serviceId, connId, socket, selfId, apiTimeout))
        {
            //记录添加失败关闭超时的连接
            socket.Close();
            Log.Error("ConnectionManager", $"Cannot record connection[{connId}]");
            return;
        }

        if (OnOpenConnectionAsync == null) return;
        long.TryParse(selfId, out var uid);
        Task.Run(async () => { await OnOpenConnectionAsync(connId, new ConnectionEventArgs(role, uid)); });
    }

    /// <summary>
    /// 服务器链接关闭事件
    /// </summary>
    /// <param name="role">通道标识</param>
    /// <param name="selfId">事件源</param>
    /// <param name="connId">id</param>
    internal void CloseConnection(string role, long selfId, Guid connId)
    {
        //关闭连接
        try
        {
            StaticVariable.ConnectionInfos[connId].Connection.Close();
        }
        catch (Exception e)
        {
            Log.Error("ConnectionManager", "Close connection failed");
            Log.Error("ConnectionManager", Log.ErrorLogBuilder(e));
        }

        //移除连接信息
        if (!RemoveConnection(connId))
            Log.Error("ConnectionManager", "Remove connection record failed");
        //触发事件
        if (OnCloseConnectionAsync == null) return;
        Task.Run(async () => { await OnCloseConnectionAsync(connId, new ConnectionEventArgs(role, selfId)); });
    }

    internal static void UpdateUid(Guid connectionGuid, long uid)
    {
        var oldInfo = StaticVariable.ConnectionInfos[connectionGuid];
        var newInfo = oldInfo;
        newInfo.SelfId = uid;
        StaticVariable.ConnectionInfos.TryUpdate(connectionGuid, newInfo, oldInfo);
    }

    #endregion

    #region API

    /// <summary>
    /// 获取当前登录连接的账号ID
    /// </summary>
    /// <param name="connectionGuid">连接标识</param>
    /// <param name="userId">UID</param>
    internal static bool GetLoginUid(Guid connectionGuid, out long userId)
    {
        if (StaticVariable.ConnectionInfos.ContainsKey(connectionGuid))
        {
            userId = StaticVariable.ConnectionInfos[connectionGuid].SelfId;
            return true;
        }

        userId = -1;
        return false;
    }

    /// <summary>
    /// 获取当前连接设置的API超时
    /// </summary>
    /// <param name="connectionGuid">连接标识</param>
    /// <param name="timeout">超时</param>
    internal static bool GetApiTimeout(Guid connectionGuid, out TimeSpan timeout)
    {
        if (StaticVariable.ConnectionInfos.ContainsKey(connectionGuid))
        {
            timeout = StaticVariable.ConnectionInfos[connectionGuid].ApiTimeout;
            return true;
        }

        timeout = TimeSpan.Zero;
        return false;
    }

    #endregion

    #region 析构

    /// <summary>
    /// 析构
    /// </summary>
    ~ConnectionManager()
    {
        Dispose();
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        HeartBeatTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}