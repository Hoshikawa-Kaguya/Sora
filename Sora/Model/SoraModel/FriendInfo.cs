namespace Sora.Model.SoraModel
{
    /// <summary>
    /// 好友类
    /// </summary>
    public class FriendInfo
    {
        #region 属性
        /// <summary>
        /// 好友备注
        /// </summary>
        public string Remark { get; internal set; }

        /// <summary>
        /// 好友用户实例
        /// </summary>
        public UserInfo User { get; internal set; }
        #endregion
    }
}
