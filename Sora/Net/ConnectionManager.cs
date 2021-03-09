using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Sora.EventArgs.WSSeverEvent;
using Sora.OnebotModel;
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
            internal Guid                 ConnectionGuid;
            internal IWebSocketConnection Connection;
            internal long                 LastHeartBeatTime;
            internal long                 SelfId;
        }

        #endregion

        #region 私有字段

        /// <summary>
        /// 静态链接表
        /// </summary>
        private static readonly List<SoraConnectionInfo> ConnectionList = new();

        #endregion

        #region 属性

        private ServerConfig Config { get; }

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
            IWebSocketConnectionInfo sender, TEventArgs eventArgs) where TEventArgs : System.EventArgs;

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

        internal ConnectionManager(ServerConfig config)
        {
            Config = config;
        }

        #endregion

        #region 服务器连接管理

        /// <summary>
        /// 添加服务器连接记录
        /// </summary>
        /// <param name="connectionGuid">连接标识</param>
        /// <param name="connectionInfo">连接信息</param>
        /// <param name="selfId">机器人UID</param>
        private static bool AddConnection(Guid connectionGuid, IWebSocketConnection connectionInfo, string selfId)
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
                        LastHeartBeatTime = TimeStamp.GetNowTimeStamp(),
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
            => ConnectionList.Any(connection => connection.ConnectionGuid == connectionGuid);

        #endregion

        #region 服务器信息发送

        internal static bool SendMessage(Guid connectionGuid, string message)
        {
            if (ConnectionList.All(connection => connection.ConnectionGuid != connectionGuid)) return false;
            {
                try
                {
                    ConnectionList.Single(connection => connection.ConnectionGuid == connectionGuid).Connection
                                  .Send(message);
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
                               TimeStamp.GetNowTimeStamp() - connection.LastHeartBeatTime > Config.HeartBeatTimeOut))
                {
                    try
                    {
                        //添加需要删除的连接
                        lostConnections.Add(connection.ConnectionGuid);

                        //关闭超时的连接
                        connection.Connection.Close();
                        Log.Error("Sora",
                                  $"与Onebot客户端[{connection.Connection.ConnectionInfo.ClientIpAddress}:{connection.Connection.ConnectionInfo.ClientPort}]失去链接(心跳包超时)");
                        HeartBeatTimeOutEvent(connection.SelfId, connection.Connection.ConnectionInfo);
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
            int connectionIndex = ConnectionList.FindIndex(conn => conn.ConnectionGuid == connectionGuid);
            if (connectionIndex == -1) return;
            var connection = ConnectionList[connectionIndex];
            connection.LastHeartBeatTime    = TimeStamp.GetNowTimeStamp();
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
        internal void OpenConnection(string role, string selfId, IWebSocketConnection socket)
        {
            //添加服务器记录
            if (!AddConnection(socket.ConnectionInfo.Id, socket, selfId))
            {
                socket.Close();
                Log.Error("Sora", $"处理连接请求时发生问题 无法记录该连接[{socket.ConnectionInfo.Id}]");
                return;
            }

            if (OnOpenConnectionAsync == null) return;
            long.TryParse(selfId, out var uid);
            Task.Run(async () =>
                     {
                         await OnOpenConnectionAsync(socket.ConnectionInfo,
                                                     new ConnectionEventArgs(role, uid));
                     });
        }

        /// <summary>
        /// 服务器链接关闭事件
        /// </summary>
        /// <param name="role">通道标识</param>
        /// <param name="selfId">事件源</param>
        /// <param name="socket">连接信息</param>
        internal void CloseConnection(string role, string selfId, IWebSocketConnection socket)
        {
            if (!RemoveConnection(socket.ConnectionInfo.Id))
            {
                Log.Fatal("Sora", "客户端连接被关闭失败");
                Log.Warning("Sora", "将在5s后自动退出");
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }

            if (OnCloseConnectionAsync == null) return;
            long.TryParse(selfId, out var uid);
            Task.Run(async () =>
                     {
                         await OnCloseConnectionAsync(socket.ConnectionInfo,
                                                      new ConnectionEventArgs(role, uid));
                     });
        }

        /// <summary>
        /// 心跳包超时事件
        /// </summary>
        /// <param name="sender">连接信息</param>
        /// <param name="selfId">事件源</param>
        private void HeartBeatTimeOutEvent(long selfId, IWebSocketConnectionInfo sender)
        {
            if (OnHeartBeatTimeOut == null) return;
            Task.Run(async () =>
                     {
                         await OnHeartBeatTimeOut(sender,
                                                  new ConnectionEventArgs("unknown", selfId));
                     });
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