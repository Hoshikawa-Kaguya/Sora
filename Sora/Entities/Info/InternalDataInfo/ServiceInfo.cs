using System;
using System.Collections.Generic;
using System.Linq;
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
        internal readonly Guid ServiceId;

        /// <summary>
        /// 该服务的管理员UID
        /// </summary>
        internal readonly List<long> SuperUsers;

        /// <summary>
        /// 屏蔽用户
        /// </summary>
        internal readonly List<long> BlockUsers;

        internal ServiceInfo(Guid serviceId, ISoraConfig config)
        {
            ServiceId  = serviceId;
            SuperUsers = config.SuperUsers.ToList();
            BlockUsers = config.BlockUsers.ToList();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ServiceId, SuperUsers);
        }
    }
}