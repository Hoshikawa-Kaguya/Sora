using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Sora.Entities.Info.InternalDataInfo;
using Sora.EventArgs.WebsocketEvent;
using Sora.OnebotModel;
using Websocket.Client;
using YukariToolBox.FormatLog;

namespace Sora.Net
{
    /// <summary>
    /// 服务器连接管理器
    /// 管理服务器链接和心跳包
    /// </summary>
    public class ConnectionManager
    {
        #region 属性

        private TimeSpan HeartBeatTimeOut { get; }

        #endregion

        #region 回调事件

        /// <summary>
        /// 服务器事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数</typeparam>
        /// <param name="sender">Bot Id</param>
        /// <param name="eventArgs">事件参数</param>
        /// <returns></returns>
        public delegate ValueTask ServerAsyncCallBackHandler<in TEventArgs>(
            Guid sender, TEventArgs eventArgs) where TEventArgs : System.EventArgs;

        /// <summary>
        /// 打开连接回调
        /// </summary>
        public event ServerAsyncCallBackHandler<ConnectionEventArgs> OnOpenConnectionAsync;

        /// <summary>
        /// 关闭连接回调
        /// </summary>
        public event ServerAsyncCallBackHandler<ConnectionEventArgs> OnCloseConnectionAsync;

        /// <summary>
        /// 心跳包超时回调
        /// </summary>
        public event ServerAsyncCallBackHandler<ConnectionEventArgs> OnHeartBeatTimeOut;

        #endregion

        #region 构造函数

        internal ConnectionManager(object config)
        {
            HeartBeatTimeOut = config switch
            {
                ServerConfig server => server.HeartBeatTimeOut,
                ClientConfig client => client.HeartBeatTimeOut,
                _ => throw new NotSupportedException("unsupport config type")
            };
        }

        #endregion

        #region 服务器连接管理

        /// <summary>
        /// 添加服务器连接记录
        /// </summary>
        /// <param name="serviceId">服务Id</param>
        /// <param name="connectionId">连接标识</param>
        /// <param name="connectionInfo">连接信息</param>
        /// <param name="selfId">机器人UID</param>
        /// <param name="apiTimeout">api超时</param>
        private bool AddConnection(Guid serviceId, Guid connectionId, object connectionInfo,
                                   string selfId, TimeSpan apiTimeout)
        {
            //检查是否已存在值
            if (StaticVariable.ConnectionInfos.ContainsKey(connectionId)) return false;
            long.TryParse(selfId, out var uid);
            return StaticVariable.ConnectionInfos.TryAdd(connectionId, new SoraConnectionInfo
                                                             (serviceId: serviceId,
                                                              connection: connectionInfo,
                                                              lastHeartBeatTime: DateTime.Now,
                                                              selfId: uid,
                                                              apiTimeout: apiTimeout
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
        internal bool ConnectionExitis(Guid connectionId)
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
                switch (StaticVariable.ConnectionInfos[connectionId].Connection)
                {
                    case IWebSocketConnection serverConnection:
                        serverConnection.Send(message);
                        return true;
                    case WebsocketClient client:
                        client.SendInstant(message);
                        return true;
                    default:
                        Log.Error("ConnectionManager", "unknown error when get Connection instance");
                        return false;
                }
            }
            catch (Exception e)
            {
                Log.Error("Sora", $"Send message to client error\r\n{Log.ErrorLogBuilder(e)}");
                return false;
            }
        }

        #endregion

        #region 心跳包事件

        /// <summary>
        /// 心跳包超时检查
        /// </summary>
        internal void HeartBeatCheck(object obj)
        {
            if (StaticVariable.ConnectionInfos.IsEmpty) return;
            Log.Debug("HeartBeatCheck", $"Connection count={StaticVariable.ConnectionInfos.Count}");

            //查找超时连接
            Dictionary<Guid, SoraConnectionInfo> timeoutDict =
                StaticVariable.ConnectionInfos
                              .Where(conn =>
                                         DateTime.Now - conn.Value.LastHeartBeatTime > HeartBeatTimeOut)
                              .ToDictionary(conn => conn.Key,
                                            conn => conn.Value);

            //遍历超时的连接
            foreach (var (connection, info) in timeoutDict)
            {
                try
                {
                    switch (info.Connection)
                    {
                        case IWebSocketConnection serverConnection:
                            serverConnection.Close();
                            Log.Error("Sora",
                                      $"与Onebot客户端[{serverConnection.ConnectionInfo.ClientIpAddress}:{serverConnection.ConnectionInfo.ClientPort}]失去链接(心跳包超时)");
                            HeartBeatTimeOutEvent(info.SelfId, connection);
                            break;
                        case WebsocketClient client:
                            Log.Error("Sora",
                                      "与Onebot服务器失去链接(心跳包超时)");
                            HeartBeatTimeOutEvent(info.SelfId, connection);
                            //尝试重连
                            client.Reconnect();
                            break;
                        default:
                            Log.Error("ConnectionManager", "unknown error when get Connection instance");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Sora", "检查心跳包时发生错误(关闭超时连接时发生错误)");
                    Log.Error("Sora", Log.ErrorLogBuilder(e));
                }

                if (!StaticVariable.ConnectionInfos.TryRemove(connection, out _))
                {
                    Log.Error("Sora", "检查心跳包时发生错误(删除超时连接时发生错误)");
                }
            }
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
        internal void OpenConnection(string role, string selfId, object socket, Guid serviceId, Guid connId,
                                     TimeSpan apiTimeout)
        {
            //添加服务器记录
            if (!AddConnection(serviceId, connId, socket, selfId, apiTimeout))
            {
                //记录添加失败关闭超时的连接
                switch (socket)
                {
                    case IWebSocketConnection serverConn:
                        serverConn.Close();
                        Log.Error("Sora", $"处理连接请求时发生问题 无法记录该连接[{serverConn.ConnectionInfo.Id}]");
                        break;
                    case WebsocketClient client:
                        client.Stop(WebSocketCloseStatus.Empty, "cannot add client to list");
                        Log.Error("Sora", $"处理连接请求时发生问题 无法记录该连接[{connId}]");
                        break;
                    default:
                        Log.Error("ConnectionManager", "unknown error when get Connection instance");
                        break;
                }

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
        /// <param name="id">id</param>
        internal void CloseConnection(string role, long selfId, Guid id)
        {
            if (!RemoveConnection(id))
            {
                Log.Fatal("Sora", "Websocket连接被关闭失败");
                Log.Warning("Sora", "将在5s后自动退出");
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }

            if (OnCloseConnectionAsync == null) return;
            Task.Run(async () => { await OnCloseConnectionAsync(id, new ConnectionEventArgs(role, selfId)); });
        }

        internal static void UpdateUid(Guid connectionGuid, long uid)
        {
            var oldInfo = StaticVariable.ConnectionInfos[connectionGuid];
            var newInfo = oldInfo;
            newInfo.SelfId = uid;
            StaticVariable.ConnectionInfos.TryUpdate(connectionGuid, newInfo, oldInfo);
        }

        /// <summary>
        /// 心跳包超时事件
        /// </summary>
        /// <param name="id">连接标识</param>
        /// <param name="selfId">事件源</param>
        private void HeartBeatTimeOutEvent(long selfId, Guid id)
        {
            if (OnHeartBeatTimeOut == null) return;
            Task.Run(async () => { await OnHeartBeatTimeOut(id, new ConnectionEventArgs("unknown", selfId)); });
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
    }
}