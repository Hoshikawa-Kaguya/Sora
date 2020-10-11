using System;
using Sora.Module.SoraModel.Base;

namespace Sora.Module.SoraModel
{
    public sealed class Group : BaseModel
    {
        #region 属性
        /// <summary>
        /// 群号
        /// </summary>
        public long Id { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器连接标识</param>
        /// <param name="gid">群号</param>
        internal Group(Guid connectionGuid, long gid) : base(connectionGuid)
        {
            this.Id = gid;
        }
        #endregion
    }
}
