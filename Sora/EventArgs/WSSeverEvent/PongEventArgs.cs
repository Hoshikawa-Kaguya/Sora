using Fleck;

namespace Sora.EventArgs.WSSeverEvent
{
    public sealed class PongEventArgs : ServerBaseEventArgs
    {
        #region 属性
        /// <summary>
        /// 心跳包信息
        /// </summary>
        public byte[] Echo { get; set; }
        #endregion

        #region 构造函数
        public PongEventArgs(byte[] echo, IWebSocketConnectionInfo connectionInfo)
        {
            this.Echo           = echo;
            base.ConnectionInfo = connectionInfo;
        }
        #endregion
    }
}
