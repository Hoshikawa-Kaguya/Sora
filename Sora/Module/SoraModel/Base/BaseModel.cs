using System;
using Sora.Module.SoraModel.Info;

namespace Sora.Module.SoraModel.Base
{
    /// <summary>
    /// 数据模型基类
    /// </summary>
    public abstract class BaseModel
    {
        #region 属性
        /// <summary>
        /// API执行实例
        /// </summary>
        public SoraApi SoraApi { get; private set; }
        #endregion

        #region 构造函数
        internal BaseModel(Guid connectionGuid)
        {
            this.SoraApi = new SoraApi(connectionGuid);
        }
        #endregion
    }
}
