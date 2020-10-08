using System;

namespace Sora.Model.SoraModel
{
    public class Group
    {
        #region 属性
        /// <summary>
        /// 群号
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
