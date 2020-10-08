using Newtonsoft.Json;

namespace Sora.Model.SoraModel
{
    /// <summary>
    /// 用户类
    /// </summary>
    public class UserInfo
    {
        #region 属性
        /// <summary>
        /// 当前实例的QQ号
        /// </summary>
        public long Id { get; internal set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Nick { get; internal set; }
        #endregion
    }
}
