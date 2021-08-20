using System;
using System.Collections.Generic;
using Sora.Interfaces;

namespace Sora.Entities.Info.InternalDataInfo
{
    /// <summary>
    /// Sora服务数据结构体
    /// </summary>
    internal readonly struct ServiceInfo
    {
        /// <summary>
        /// 服务ID
        /// </summary>
        private readonly Guid ServiceId;

        /// <summary>
        /// 该服务的管理员UID
        /// </summary>
        internal readonly HashSet<long> SuperUsers;

        /// <summary>
        /// 屏蔽用户
        /// </summary>
        internal readonly HashSet<long> BlockUsers;

        /// <summary>
        /// 是否已启用指令服务
        /// </summary>
        internal readonly bool EnableSoraCommandManager;

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
}