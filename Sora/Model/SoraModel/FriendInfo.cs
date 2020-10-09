namespace Sora.Model.SoraModel
{
    /// <summary>
    /// 好友类
    /// </summary>
    public sealed class FriendInfo
    {
        #region 属性
        /// <summary>
        /// 好友备注
        /// </summary>
        public string Remark { get; internal set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Nick { get; internal set; }

        /// <summary>
        /// 好友用户实例
        /// </summary>
        public User User { get; internal set; }
        #endregion
    }
}
