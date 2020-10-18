using Fleck;

namespace Sora.Module.Info
{
    /// <summary>
    /// 连接信息结构体
    /// </summary>
    internal struct ConnectionInfo
    {
        /// <summary>
        /// 连接的账号id
        /// </summary>
        internal long SelfId { get; set; }

        /// <summary>
        /// 连接信息
        /// </summary>
        internal IWebSocketConnection ServerConnection { get; set; }
    }
}
