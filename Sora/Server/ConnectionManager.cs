using System;
using System.Collections.Generic;
using System.Linq;
using Fleck;
using Sora.Tool;

namespace Sora.Server
{
    /// <summary>
    /// 服务器连接管理器
    /// 管理服务器链接和心跳包
    /// </summary>
    internal class ConnectionManager
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

        #region 构造函数

        internal ConnectionManager(ServerConfig config)
        {
            Config = config;
        }
        #endregion

        #region 服务器事件

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

        internal bool RemoveConnection(Guid connectionGuid)
        {
            if (ConnectionList.Any(connection => connection.ConnectionGuid == connectionGuid))
            {
                return ConnectionList.RemoveAll(connection => connection.ConnectionGuid == connectionGuid) > 0;
            }
            else return false;
        }

        internal bool ConnectionExitis(Guid connectionGuid)
            => ConnectionList.Any(connection => connection.ConnectionGuid == connectionGuid);

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

        #region 刷新心跳包
        internal static void HeartBeatUpdate(Guid connectionGuid)
        {
           int connectionIndex = ConnectionList.FindIndex(conn => conn.ConnectionGuid == connectionGuid);
           var connection      = ConnectionList[connectionIndex];
           connection.LastHeartBeatTime    = Utils.GetNowTimeStamp();
           ConnectionList[connectionIndex] = connection;
        }
        #endregion
    }
}
