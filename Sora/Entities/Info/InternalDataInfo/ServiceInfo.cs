using System;
using System.Collections.Generic;
using Sora.Interfaces;

namespace Sora.Entities.Info.InternalDataInfo;

/// <summary>
/// Sora服务数据结构体
/// </summary>
internal readonly struct ServiceInfo
{
    private readonly  Guid          ServiceId;
    internal readonly HashSet<long> SuperUsers;
    internal readonly HashSet<long> BlockUsers;
    internal readonly bool          EnableSoraCommandManager;

    internal ServiceInfo(Guid serviceId, ISoraConfig config)
    {
        ServiceId                = serviceId;
        EnableSoraCommandManager = config.EnableSoraCommandManager;
        SuperUsers               = new HashSet<long>(config.SuperUsers);
        BlockUsers               = new HashSet<long>(config.BlockUsers);
    }

    public override int GetHashCode()
    {
        return ServiceId.GetHashCode();
    }
}