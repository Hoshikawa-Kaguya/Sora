using System;

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

        internal ServiceInfo(Guid serviceId, long[] superUsers)
        {
            ServiceId  = serviceId;
            SuperUsers = superUsers;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ServiceId, SuperUsers);
        }
    }
}