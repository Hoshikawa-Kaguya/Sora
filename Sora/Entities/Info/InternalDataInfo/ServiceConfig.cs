using System.Collections.Generic;
using Sora.Interfaces;

namespace Sora.Entities.Info.InternalDataInfo;

/// <summary>
/// Sora服务数据结构体
/// </summary>
internal readonly struct ServiceConfig
{
    internal readonly HashSet<long> SuperUsers;
    internal readonly HashSet<long> BlockUsers;
    internal readonly bool          EnableSoraCommandManager;
    internal readonly bool          EnableSocketMessage;
    internal readonly bool          AutoMarkMessageRead;

    internal ServiceConfig(ISoraConfig config)
    {
        EnableSoraCommandManager = config.EnableSoraCommandManager;
        EnableSocketMessage      = config.EnableSocketMessage;
        AutoMarkMessageRead      = config.AutoMarkMessageRead;
        SuperUsers               = new HashSet<long>(config.SuperUsers);
        BlockUsers               = new HashSet<long>(config.BlockUsers);
    }
}