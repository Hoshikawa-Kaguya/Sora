using System;
using Sora.Interfaces;

namespace Sora.Entities.Info.InternalDataInfo;

/// <summary>
/// 用于存储链接信息和心跳时间的结构体
/// </summary>
internal struct SoraConnectionInfo
{
    internal readonly Guid        ServiceId;
    private readonly  Guid        ConnectionId;
    internal readonly ISoraSocket Connection;
    internal          DateTime    LastHeartBeatTime;
    internal          long        SelfId;
    internal readonly TimeSpan    ApiTimeout;

    internal SoraConnectionInfo(Guid serviceId, Guid connectionId, ISoraSocket connection,
                                DateTime lastHeartBeatTime, long selfId,
                                TimeSpan apiTimeout)
    {
        ServiceId         = serviceId;
        Connection        = connection;
        LastHeartBeatTime = lastHeartBeatTime;
        SelfId            = selfId;
        ApiTimeout        = apiTimeout;
        ConnectionId      = connectionId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ServiceId, ConnectionId);
    }
}