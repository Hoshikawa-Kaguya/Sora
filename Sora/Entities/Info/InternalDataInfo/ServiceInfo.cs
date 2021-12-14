using System;
using System.Collections.Generic;
using Sora.Interfaces;

namespace Sora.Entities.Info.InternalDataInfo;

/// <summary>
/// Sora服务数据结构体
/// </summary>
internal readonly struct ServiceInfo
{
    private readonly  Guid           ServiceId;
    internal readonly HashSet<long>  GroupSuperUsers;
    internal readonly HashSet<long>  GroupBlockUsers;
    internal readonly HashSet<ulong> GuildSuperUsers;
    internal readonly HashSet<ulong> GuildBlockUsers;
    internal readonly bool           EnableSoraCommandManager;

    internal ServiceInfo(Guid serviceId, ISoraConfig config)
    {
        ServiceId                = serviceId;
        EnableSoraCommandManager = config.EnableSoraCommandManager;
        GroupSuperUsers          = new HashSet<long>(config.GroupSuperUsers);
        GroupBlockUsers          = new HashSet<long>(config.GroupBlockUsers);
        GuildSuperUsers          = new HashSet<ulong>(config.GuildSuperUsers);
        GuildBlockUsers          = new HashSet<ulong>(config.GuildBlockUsers);
    }

    public override int GetHashCode()
    {
        return ServiceId.GetHashCode();
    }
}