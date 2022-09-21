using System;
using Sora.Entities.Base;
using Sora.Interfaces;

namespace Sora.Entities.Info.InternalDataInfo;

/// <summary>
/// 用于存储链接信息和心跳时间的结构体
/// </summary>
internal struct SoraConnectionInfo
{
    private readonly  Lazy<SoraApi> _apiInstance;
    internal readonly ISoraSocket   Connection;
    internal          DateTime      LastHeartBeatTime;
    internal          long          LoginUid;

    internal readonly TimeSpan ApiTimeout;
    // internal readonly Dictionary<long, string>

    internal readonly SoraApi ApiInstance => _apiInstance.Value;


    internal SoraConnectionInfo(Guid        serviceId,
                                Guid        connId,
                                ISoraSocket connection,
                                DateTime    lastHeartBeatTime,
                                TimeSpan    apiTimeout)
    {
        Connection        = connection;
        LastHeartBeatTime = lastHeartBeatTime;
        LoginUid          = 0;
        ApiTimeout        = apiTimeout;
        _apiInstance      = new Lazy<SoraApi>(() => new SoraApi(serviceId, connId));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ApiInstance.ConnectionId, ApiInstance.ServiceId);
    }
}