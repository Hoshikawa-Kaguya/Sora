using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Sora.EventArgs.WebsocketEvent;
using Sora.OnebotModel;
using Websocket.Client;
using YukariToolBox.FormatLog;
using YukariToolBox.Time;

namespace Sora.Net
{
    /// <summary>
    /// 服务器连接管理器
    /// 管理服务器链接和心跳包
    /// </summary>
    public class ConnectionManager
    {
        #region 数据结构体

        /// <summary>
        /// 用于存储链接信息和心跳时间的结构体
        /// </summary>
        private struct SoraConnectionInfo
        {
            internal Guid     ConnectionGuid;
            internal object   Connection;
            internal DateTime LastHeartBeatTime;
            internal long     SelfId;
        }

        #endregion

        #region 私有字段

        /// <summary>
        /// 静态链接表
        /// </summary>
        private static readonly List<SoraConnectionInfo> ConnectionList = new();

        #endregion

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
        /// <param name="connectionGuid">连接标识</param>
        /// <param name="connectionInfo">连接信息</param>
        /// <param name="selfId">机器人UID</param>
        private static bool AddConnection(Guid connectionGuid, object connectionInfo, string selfId)
        {
            //锁定记录表
            lock (ConnectionList)
            {
                //检查是否已存在值
                if (ConnectionList.All(connection => connection.ConnectionGuid != connectionGuid))
                {
                    long.TryParse(selfId, out var uid);
                    ConnectionList.Add(new SoraConnectionInfo
                    {
                        ConnectionGuid    = connectionGuid,
                        Connection        = connectionInfo,
                        LastHeartBeatTime = DateTime.Now,
                        SelfId            = uid
                    });
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 移除服务器连接记录
        /// </summary>
        /// <param name="connectionGuid">连接标识</param>
        private static bool RemoveConnection(Guid connectionGuid)
        {
            //锁定记录表
            lock (ConnectionList)
            {
                if (ConnectionList.Any(connection => connection.ConnectionGuid == connectionGuid))
                {
                    return ConnectionList.RemoveAll(connection => connection.ConnectionGuid == connectionGuid) > 0;
                }

                return false;
            }
        }

        /// <summary>
        /// 检查是否存在连接
        /// </summary>
        /// <param name="connectionGuid">连接标识</param>
        internal static bool ConnectionExitis(Guid connectionGuid)
        {
            lock (ConnectionList)
            {
                return ConnectionList.Any(connection => connection.ConnectionGuid == connectionGuid);
            }
        }

        #endregion

        #region 服务器信息发送

        internal static bool SendMessage(Guid connectionGuid, string message)
        {
            if (ConnectionList.All(connection => connection.ConnectionGuid != connectionGuid)) return false;
            {
                try
                {
                    switch (ConnectionList.Single(connection => connection.ConnectionGuid == connectionGuid).Connection)
                    {
                        case IWebSocketConnection serverConnection:
                            serverConnection.Send(message);
                            break;
                        case WebsocketClient client:
                            client.Send(message);
                            break;
                        default:
                            Log.Error("ConnectionManager", "unknown error when get Connection instance");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Sora", $"Send message to client error\r\n{Log.ErrorLogBuilder(e)}");
                }

                return true;
            }
        }

        #endregion

        #region 心跳包事件

        /// <summary>
        /// 心跳包超时检查
        /// </summary>
        internal void HeartBeatCheck(object obj)
        {
            if (ConnectionList.Count == 0) return;
            Log.Debug("HeartBeatCheck", $"Connection count={ConnectionList.Count}");
            List<Guid> lostConnections = new();
            //锁定列表
            lock (ConnectionList)
            {
                //遍历超时的连接
                foreach (var connection in ConnectionList
                    .Where(connection =>
                               DateTime.Now - connection.LastHeartBeatTime > HeartBeatTimeOut))
                {
                    try
                    {
                        //添加需要删除的连接
                        lostConnections.Add(connection.ConnectionGuid);
                        //关闭超时的连接
                        switch (connection.Connection)
                        {
                            case IWebSocketConnection serverConnection:
                                serverConnection.Close();
                                Log.Error("Sora",
                                          $"与Onebot客户端[{serverConnection.ConnectionInfo.ClientIpAddress}:{serverConnection.ConnectionInfo.ClientPort}]失去链接(心跳包超时)");
                                HeartBeatTimeOutEvent(connection.SelfId, connection.ConnectionGuid);
                                break;
                            case WebsocketClient client:
                                Log.Error("Sora",
                                          "与Onebot服务器失去链接(心跳包超时)");
                                HeartBeatTimeOutEvent(connection.SelfId, connection.ConnectionGuid);
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
                        Log.Error("Sora", "检查心跳包时发生错误 code -2");
                        Log.Error("Sora", Log.ErrorLogBuilder(e));
                        //添加需要删除的连接
                        lostConnections.Add(connection.ConnectionGuid);
                    }
                }
            }

            //删除超时的连接
            foreach (var lostConnection in lostConnections
                .Where(lostConnection => !RemoveConnection(lostConnection)))
            {
                Log.Error("Sora", $"检查心跳包时发生错误 code -1, 连接[{lostConnection}]无法被关闭");
            }

            //清理无效API请求
            ReactiveApiManager.CleanApiReqList();
        }

        /// <summary>
        /// 刷新心跳包记录
        /// </summary>
        /// <param name="connectionGuid">连接标识</param>
        internal static void HeartBeatUpdate(Guid connectionGuid)
        {
            var connectionIndex = ConnectionList.FindIndex(conn => conn.ConnectionGuid == connectionGuid);
            if (connectionIndex == -1) return;
            var connection = ConnectionList[connectionIndex];
            connection.LastHeartBeatTime    = DateTime.Now;
            ConnectionList[connectionIndex] = connection;
        }

        #endregion

        #region 服务器事件

        /// <summary>
        /// 服务器链接开启事件
        /// </summary>
        /// <param name="role">通道标识</param>
        /// <param name="selfId">事件源</param>
        /// <param name="socket">连接</param>
        /// <param name="id">ID</param>
        internal void OpenConnection(string role, string selfId, object socket, Guid id)
        {
            //添加服务器记录
            if (!AddConnection(id, socket, selfId))
            {
                //关闭超时的连接
                switch (socket)
                {
                    case IWebSocketConnection serverConn:
                        serverConn.Close();
                        Log.Error("Sora", $"处理连接请求时发生问题 无法记录该连接[{serverConn.ConnectionInfo.Id}]");
                        break;
                    case WebsocketClient client:
                        client.Stop(WebSocketCloseStatus.Empty, "cannot add client to list");
                        Log.Error("Sora", $"处理连接请求时发生问题 无法记录该连接[{id}]");
                        break;
                    default:
                        Log.Error("ConnectionManager", "unknown error when get Connection instance");
                        break;
                }

                return;
            }

            if (OnOpenConnectionAsync == null) return;
            long.TryParse(selfId, out var uid);
            Task.Run(async () => { await OnOpenConnectionAsync(id, new ConnectionEventArgs(role, uid)); });
        }

        /// <summary>
        /// 服务器链接关闭事件
        /// </summary>
        /// <param name="role">通道标识</param>
        /// <param name="selfId">事件源</param>
        /// <param name="id">id</param>
        internal void CloseConnection(string role, string selfId, Guid id)
        {
            if (!RemoveConnection(id))
            {
                Log.Fatal("Sora", "Websocket连接被关闭失败");
                Log.Warning("Sora", "将在5s后自动退出");
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }

            if (OnCloseConnectionAsync == null) return;
            long.TryParse(selfId, out var uid);
            Task.Run(async () => { await OnCloseConnectionAsync(id, new ConnectionEventArgs(role, uid)); });
        }

        internal static void UpdateUid(Guid connectionGuid, long uid)
        {
            var connectionIndex = ConnectionList.FindIndex(conn => conn.ConnectionGuid == connectionGuid);
            if (connectionIndex == -1) return;
            var connection = ConnectionList[connectionIndex];
            connection.SelfId               = uid;
            ConnectionList[connectionIndex] = connection;
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

        internal static bool GetLoginUid(Guid connectionGuid, out long userId)
        {
            if (ConnectionList.Any(conn => conn.ConnectionGuid == connectionGuid))
            {
                userId = ConnectionList.Where(conn => conn.ConnectionGuid == connectionGuid)
                                       .Select(conn => conn.SelfId)
                                       .FirstOrDefault();
                return true;
            }
            else
            {
                userId = -1;
                return false;
            }
        }

        #endregion
    }
}