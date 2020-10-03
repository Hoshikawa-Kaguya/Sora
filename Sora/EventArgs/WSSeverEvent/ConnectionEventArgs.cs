using Fleck;

namespace Sora.EventArgs.WSSeverEvent
{
    /// <summary>
    /// 服务器连接事件
    /// </summary>
    public sealed class ConnectionEventArgs : ServerBaseEventArgs
    {
        #region 属性
        /// <summary>
        /// 客户端类型
        /// </summary>
        public string Role { get; set; }
        #endregion

        #region 构造函数
        internal ConnectionEventArgs(string role, IWebSocketConnectionInfo connectionInfo)
        {
            
            this.Role           = role;
            base.ConnectionInfo = connectionInfo;
        }
        #endregion
    }
}
