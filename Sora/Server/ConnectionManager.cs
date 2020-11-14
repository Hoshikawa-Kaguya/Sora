using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fleck;
using Sora.EventArgs.WSSeverEvent;
using Sora.Tool;

namespace Sora.Server
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
        }
        #endregion

        #region 私有字段
        /// <summary>
        /// 静态链接表
        /// </summary>
        private static readonly List<SoraConnectionInfo> ConnectionList = new List<SoraConnectionInfo>();
        #endregion

        #region 属性
        private ServerConfig Config { get; set; }
        #endregion

        /// <summary>
        /// 服务器事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数</typeparam>
        /// <param name="sender">Bot Id</param>
        /// <param name="eventArgs">事件参数</param>
        /// <returns></returns>
        public delegate ValueTask ServerAsyncCallBackHandler<in TEventArgs>(IWebSocketConnectionInfo sender, TEventArgs eventArgs)where TEventArgs : System.EventArgs;

        #region 回调事件
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
        internal bool AddConnection(Guid connectionGuid, IWebSocketConnection connectionInfo)
        {
            //检查是否已存在值
            if (ConnectionList.All(connection => connection.ConnectionGuid != connectionGuid))
            {
                ConnectionList.Add(new SoraConnectionInfo
                {
                    ConnectionGuid    = connectionGuid,
                    Connection        = connectionInfo,
                    LastHeartBeatTime = Utils.GetNowTimeStamp()
                });
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 移除服务器连接记录
        /// </summary>
        /// <param name="connectionGuid">连接标识</param>
        internal bool RemoveConnection(Guid connectionGuid)
        {
            if (ConnectionList.Any(connection => connection.ConnectionGuid == connectionGuid))
            {
                return ConnectionList.RemoveAll(connection => connection.ConnectionGuid == connectionGuid) > 0;
            }
            else return false;
        }

        /// <summary>
        /// 检查是否存在连接
        /// </summary>
        /// <param name="connectionGuid">连接标识</param>
        internal bool ConnectionExitis(Guid connectionGuid)
            => ConnectionList.Any(connection => connection.ConnectionGuid == connectionGuid);
        #endregion

        #region 服务器信息发送
        internal static bool SendMessage(Guid connectionGuid, string message)
        {
            if (ConnectionList.Any(connection => connection.ConnectionGuid == connectionGuid))
            { 
                ConnectionList.Single(connection => connection.ConnectionGuid == connectionGuid).Connection.Send(message);
                return true;
            }
            return false;
        }
        #endregion

        #region 心跳包事件
        /// <summary>
        /// 心跳包超时检查
        /// </summary>
        internal void HeartBeatCheck(object msg)
        {
            if(ConnectionList.Count == 0) return;
            List<Guid> lostConnections = new List<Guid>();
            //遍历超时的连接
            foreach (var connection in ConnectionList
                .Where(connection => Utils.GetNowTimeStamp() - connection.LastHeartBeatTime > Config.HeartBeatTimeOut))
            {
                try
                {
                    //添加需要删除的连接
                    lostConnections.Add(connection.ConnectionGuid);

                    //关闭超时的连接
                    connection.Connection.Close();
                    ConsoleLog.Error("Sora",
                                     $"与Onebot客户端[{connection.Connection.ConnectionInfo.ClientIpAddress}:{connection.Connection.ConnectionInfo.ClientPort}]失去链接(心跳包超时)");
                    HeartBeatTimeOutEvent(connection.Connection.ConnectionInfo);
                }
                catch (Exception e)
                {
                    ConsoleLog.Error("Sora","检查心跳包时发生错误 code -2");
                    ConsoleLog.Error("Sora",ConsoleLog.ErrorLogBuilder(e));
                    //添加需要删除的连接
                    lostConnections.Add(connection.ConnectionGuid);
                }
            }
            //删除超时的连接
            foreach (var lostConnection in lostConnections
                .Where(lostConnection => !RemoveConnection(lostConnection)))
            {
                ConsoleLog.Error("Sora",$"检查心跳包时发生错误 code -1, 连接[{lostConnection}]无法被关闭");
            }
        }

        /// <summary>
        /// 刷新心跳包记录
        /// </summary>
        /// <param name="connectionGuid">连接标识</param>
        internal static void HeartBeatUpdate(Guid connectionGuid)
        {
           int connectionIndex = ConnectionList.FindIndex(conn => conn.ConnectionGuid == connectionGuid);
           var connection      = ConnectionList[connectionIndex];
           connection.LastHeartBeatTime    = Utils.GetNowTimeStamp();
           ConnectionList[connectionIndex] = connection;
        }
        #endregion

        #region 服务器事件
        /// <summary>
        /// 服务器链接开启事件
        /// </summary>
        /// <param name="role">通道标识</param>
        /// <param name="sender">事件源</param>
        internal void OpenConnectionEvent(string role, IWebSocketConnectionInfo sender)
        {
            if(OnOpenConnectionAsync == null) return;
            Task.Run(async () =>
                     {
                         await OnOpenConnectionAsync(sender,
                                                     new ConnectionEventArgs(role, sender));
                     });
        }

        /// <summary>
        /// 服务器链接关闭事件
        /// </summary>
        /// <param name="role">通道标识</param>
        /// <param name="sender">事件源</param>
        internal void CloseConnectionEvent(string role, IWebSocketConnectionInfo sender)
        {
            if(OnCloseConnectionAsync == null) return;
            Task.Run(async () =>
                     {
                         await OnCloseConnectionAsync(sender,
                                                      new ConnectionEventArgs(role, sender));
                     });
        }

        /// <summary>
        /// 心跳包超时事件
        /// </summary>
        /// <param name="sender">事件源</param>
        internal void HeartBeatTimeOutEvent(IWebSocketConnectionInfo sender)
        {
            if(OnHeartBeatTimeOut == null) return;
            Task.Run(async () =>
                     {
                         await OnHeartBeatTimeOut(sender,
                                                  new ConnectionEventArgs("unknown", sender));
                     });
        }
        #endregion
    }
}
