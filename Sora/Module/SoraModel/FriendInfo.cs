using System;

namespace Sora.Module.SoraModel
{
    /// <summary>
    /// 好友类
    /// </summary>
    public sealed class FriendInfo
    {
        #region 属性
        /// <summary>
        /// 服务器链接GUID
        /// 用于构建用户实例
        /// </summary>
        internal Guid ConnectionGuid { get; set; }

        /// <summary>
        /// 好友备注
        /// </summary>
        public string Remark { get; internal set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Nick { get; internal set; }

        /// <summary>
        /// 好友ID
        /// </summary>
        public long UserId { get; internal set; }
        #endregion

        #region 公有方法
        /// <summary>
        /// 获取用户实例
        /// </summary>
        public User GetUser()
        {
            return new User(this.ConnectionGuid, UserId);
        }
        #endregion
    }
}
