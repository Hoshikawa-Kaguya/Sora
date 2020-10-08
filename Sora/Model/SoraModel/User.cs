using System;

namespace Sora.Model.SoraModel
{
    /// <summary>
    /// 用户类
    /// </summary>
    public class User
    {
        #region 属性
        /// <summary>
        /// 当前实例的QQ号
        /// </summary>
        public long Id { get; internal set; }

        /// <summary>
        /// 当前实例对应的链接GUID
        /// 用于调用API
        /// </summary>
        internal Guid ConnectionGuid { get; set; }
        #endregion
    }
}
