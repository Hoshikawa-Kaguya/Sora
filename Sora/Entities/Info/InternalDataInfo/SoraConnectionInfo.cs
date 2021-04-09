using System;
using Fleck;
using Websocket.Client;

namespace Sora.Entities.Info.InternalDataInfo
{
    /// <summary>
    /// 用于存储链接信息和心跳时间的结构体
    /// </summary>
    internal struct SoraConnectionInfo
    {
        internal readonly Guid     ServiceId;
        internal readonly object   Connection;
        internal          DateTime LastHeartBeatTime;
        internal          long     SelfId;
        internal readonly TimeSpan ApiTimeout;
        private readonly  int      HashCode;

        internal SoraConnectionInfo(Guid serviceId, object connection, DateTime lastHeartBeatTime, long selfId,
                                    TimeSpan apiTimeout)
        {
            ServiceId         = serviceId;
            Connection        = connection;
            LastHeartBeatTime = lastHeartBeatTime;
            SelfId            = selfId;
            ApiTimeout        = apiTimeout;
            HashCode = connection switch
            {
                IWebSocketConnection serverConnection => System.HashCode.Combine(ServiceId,
                    serverConnection.ConnectionInfo.Id.GetHashCode()),
                WebsocketClient client => System.HashCode.Combine(ServiceId, client.GetHashCode()),
                _ => throw new NotSupportedException("unknown connection type")
            };
        }

        public override int GetHashCode()
        {
            return HashCode;
        }
    }
}