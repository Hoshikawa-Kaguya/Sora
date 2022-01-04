using System;
using Sora.Interfaces;

namespace Sora.Entities.Info.InternalDataInfo;

/// <summary>
/// 用于存储链接信息和心跳时间的结构体
/// </summary>
internal struct SoraConnectionInfo
{
    internal readonly Guid        ServiceId;
    internal readonly ISoraSocket Connection;
    internal          DateTime    LastHeartBeatTime;
    internal          long        SelfId;
    internal readonly TimeSpan    ApiTimeout;

    internal SoraConnectionInfo(Guid serviceId,         ISoraSocket connection,
        DateTime                     lastHeartBeatTime, TimeSpan    apiTimeout)
    {
        ServiceId         = serviceId;
        Connection        = connection;
        LastHeartBeatTime = lastHeartBeatTime;
        SelfId            = 0;
        ApiTimeout        = apiTimeout;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ServiceId);
    }
}