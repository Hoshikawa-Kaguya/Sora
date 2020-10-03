using Fleck;
using Sora.TypeEnum;

namespace Sora.EventArgs.WSSeverEvent
{
    public sealed class ConnectionEventArgs : ServerBaseEventArgs
    {
        #region 属性
        /// <summary>
        /// 客户端类型
        /// </summary>
        public ConnectionType Role { get; set; }
        #endregion

        #region 构造函数
        public ConnectionEventArgs(ConnectionType role, IWebSocketConnectionInfo connectionInfo)
        {
            
            this.Role           = role;
            base.ConnectionInfo = connectionInfo;
        }
        #endregion
    }
}
