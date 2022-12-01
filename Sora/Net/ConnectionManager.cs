using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;
using Sora.EventArgs.WebsocketEvent;
using Sora.Interfaces;
using Sora.Net.Records;
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

    private Timer HeartBeatTimer { get; }

#endregion

#region 回调事件

    /// <summary>
    /// 服务器事件回调
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型</typeparam>
    /// <param name="connectionId">连接Id</param>
    /// <param name="eventArgs">事件参数</param>
    public delegate ValueTask ServerAsyncCallBackHandler<in TEventArgs>(Guid connectionId, TEventArgs eventArgs)
        where TEventArgs : System.EventArgs;

    /// <summary>
    /// <para>打开连接回调</para>
    /// <para>注意:正向ws在链接开启时不会获取到SelfId</para>
    /// </summary>
    public event ServerAsyncCallBackHandler<ConnectionEventArgs> OnOpenConnectionAsync;

    /// <summary>
    /// 关闭连接回调
    /// </summary>
    public event ServerAsyncCallBackHandler<ConnectionEventArgs> OnCloseConnectionAsync;

#endregion

#region 构造函数

    internal ConnectionManager(ISoraConfig config, Guid serviceId)
    {
        HeartBeatTimeOut =   config.HeartBeatTimeOut;
        HeartBeatTimer   ??= new Timer(HeartBeatCheck, serviceId, HeartBeatTimeOut, HeartBeatTimeOut);
    }

#endregion

#region 服务器信息发送

    internal static bool SendMessage(Guid connectionId, string message)
    {
        try
        {
            if (!ConnectionRecord.GetConn(connectionId, out SoraConnectionInfo connection))
            {
                Log.Error("Socket", $"无法获取Socket连接[{connectionId}]");
                return false;
            }

            connection.Connection.Send(message);
            return true;
        }
        catch (Exception e)
        {
            Log.Error("Socket", $"发送WS消息时发生错误:\r\n{Log.ErrorLogBuilder(e)}");
            return false;
        }
    }

#endregion

#region 心跳包管理

    /// <summary>
    /// 心跳包超时检查
    /// </summary>
    internal void HeartBeatCheck(object serviceIdObj)
    {
        if (ConnectionRecord.IsEmpty())
            return;
        Guid serviceId = (Guid)serviceIdObj;
        Log.Verbose("HeartBeatCheck", $"service id={serviceId}({ConnectionRecord.ConnCount()})");

        //查找超时连接
        DateTime now = DateTime.Now;
        Dictionary<Guid, SoraConnectionInfo> timeoutDict =
            ConnectionRecord.GetTimeoutConn(serviceId, now, HeartBeatTimeOut);
        if (timeoutDict.Count == 0)
            return;
        Log.Warning("HeartBeatCheck", $"发现超时的连接[{timeoutDict.Count}]");

        List<WebsocketClient> needReconnect = new();
        //遍历超时的连接
        foreach ((Guid connection, SoraConnectionInfo info) in timeoutDict)
        {
            CloseConnection(connection);
            double t = (now - info.LastHeartBeatTime).TotalMilliseconds;
            Log.Error("HeartBeatCheck", $"WebSocket连接[{connection}]心跳包超时({t}ms)，断开连接");
            //客户端尝试重连
            if (info.Connection.SocketType == SoraSocketType.Client
                && info.Connection.SocketInstance is WebsocketClient c)
                needReconnect.Add(c);
        }

        if (needReconnect.Count != 0)
            needReconnect.ForEach(conn => conn.Reconnect());
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
    internal void OpenConnection(string      role,
                                 string      selfId,
                                 ISoraSocket socket,
                                 Guid        serviceId,
                                 Guid        connId,
                                 TimeSpan    apiTimeout)
    {
        //添加服务器记录
        if (!ConnectionRecord.AddNewConn(serviceId, connId, socket, apiTimeout))
        {
            //记录添加失败关闭超时的连接
            socket.Close();
            Log.Error("ConnectionManager", $"无法添加新连接[{connId}]");
            return;
        }

        if (OnOpenConnectionAsync == null)
            return;
        if (!long.TryParse(selfId, out long uid))
            Log.Error("ConnectionManager", "接收到非法selfid，已忽略");
        Task.Run(async () => { await OnOpenConnectionAsync(connId, new ConnectionEventArgs(role, uid, connId)); });
    }

    /// <summary>
    /// 服务器链接关闭事件
    /// </summary>
    /// <param name="connId">id</param>
    internal void CloseConnection(Guid connId)
    {
        if (!ConnectionRecord.GetConn(connId, out SoraConnectionInfo conn))
            return;

        ConnectionRecord.CloseConn(connId);
        //触发事件
        if (OnCloseConnectionAsync == null)
            return;
        Task.Run(async () =>
        {
            await OnCloseConnectionAsync(connId, new ConnectionEventArgs("Universal", conn.LoginUid, connId));
        });
    }

    /// <summary>
    /// 关闭服务中的所有链接
    /// </summary>
    internal void CloseAllConnection(Guid serviceId)
    {
        List<SoraConnectionInfo> connections = ConnectionRecord.GetConnList(serviceId);
        foreach (SoraConnectionInfo connection in connections)
            CloseConnection(connection.ApiInstance.ConnectionId);
    }

    /// <summary>
    /// 强制关闭链接
    /// </summary>
    internal static void ForceCloseConnection(Guid connId)
    {
        ConnectionRecord.CloseConn(connId);
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