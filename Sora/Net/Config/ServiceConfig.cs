using System.Collections.Concurrent;
using System.Collections.Generic;
using Sora.Interfaces;

namespace Sora.Net.Config;

/// <summary>
/// Sora服务数据结构体
/// </summary>
internal record ServiceConfig
{
    //控制记录
    internal readonly HashSet<long>                               SuperUsers;      //服务管理员设置
    internal readonly HashSet<long>                               BlockUsers;      //服务屏蔽用户设置
    internal readonly ConcurrentDictionary<long, HashSet<string>> GroupBanCommand; //服务群组指令控制

    //使能控制
    internal readonly bool EnableSoraCommandManager; //指令使能
    internal readonly bool EnableSocketMessage;      //控制台消息打印使能
    internal readonly bool AutoMarkMessageRead;      //自动已读标记使能

    internal ServiceConfig(ISoraConfig config)
    {
        SuperUsers      = new HashSet<long>(config.SuperUsers);
        BlockUsers      = new HashSet<long>(config.BlockUsers);
        GroupBanCommand = new ConcurrentDictionary<long, HashSet<string>>();

        EnableSoraCommandManager = config.EnableSoraCommandManager;
        EnableSocketMessage      = config.EnableSocketMessage;
        AutoMarkMessageRead      = config.AutoMarkMessageRead;
    }
}