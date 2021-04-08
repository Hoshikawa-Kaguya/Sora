using System.Linq;
using System.Net.NetworkInformation;

namespace Sora.Net
{
    /// <summary>
    /// Net工具
    /// </summary>
    internal static class NetUtils
    {
        /// <summary>
        /// 检查端口占用
        /// </summary>
        /// <param name="port">端口号</param>
        internal static bool IsPortInUse(uint port) =>
            IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
                              .Any(ipEndPoint => ipEndPoint.Port == port);
    }
}