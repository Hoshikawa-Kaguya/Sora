using System;
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
        internal readonly long[] SuperUsers;

        /// <summary>
        /// 屏蔽用户
        /// </summary>
        internal readonly long[] BlockUsers;

        internal ServiceInfo(Guid serviceId, ISoraConfig config)
        {
            ServiceId  = serviceId;
            SuperUsers = config.SuperUsers;
            BlockUsers = config.BlockUsers;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ServiceId, SuperUsers);
        }
    }
}