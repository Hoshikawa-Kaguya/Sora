using System;
using Sora.Module.SoraModel.Base;

namespace Sora.Module.SoraModel
{
    /// <summary>
    /// 用户类
    /// </summary>
    public sealed class User : BaseModel
    {
        #region 属性
        /// <summary>
        /// 当前实例的用户ID
        /// </summary>
        public long Id { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器连接标识</param>
        /// <param name="uid">用户ID</param>
        internal User(Guid connectionGuid, long uid) : base(connectionGuid)
        {
            this.Id = uid;
        }
        #endregion
    }
}
