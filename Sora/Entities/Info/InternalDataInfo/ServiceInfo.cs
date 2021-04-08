using System;

namespace Sora.Entities.Info.InternalDataInfo
{
    /// <summary>
    /// Sora服务数据结构体
    /// </summary>
    internal struct ServiceInfo
    {
        /// <summary>
        /// 服务ID
        /// </summary>
        internal Guid ServiceId;

        /// <summary>
        /// 该服务的管理员UID
        /// </summary>
        internal long[] SuperUsers;

        internal ServiceInfo(Guid serviceId, long[] superUsers)
        {
            ServiceId  = serviceId;
            SuperUsers = superUsers;
        }
    }
}