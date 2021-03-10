using System;
using System.Linq;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using YukariToolBox.FormatLog;

namespace Sora.Net
{
    /// <summary>
    /// Net工具
    /// </summary>
    internal static class NetUtils
    {
        /// <summary>
        /// 当前进程服务器已存在的标识
        /// </summary>
        public static bool serviceExitis = false;

        /// <summary>
        /// 检查端口占用
        /// </summary>
        /// <param name="port">端口号</param>
        internal static bool IsPortInUse(uint port) =>
            IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
                              .Any(ipEndPoint => ipEndPoint.Port == port);

        /// <summary>
        /// 友好的崩溃提示(x)
        /// </summary>
        internal static void FriendlyException(UnhandledExceptionEventArgs args)
        {
            var e = args.ExceptionObject as Exception;
            if (e is JsonSerializationException)
            {
                Log.Error("Sora", "Json反序列化时出现错误，可能是go-cqhttp配置出现问题。请把go-cqhttp配置中的post_message_format从string改为array。");
            }

            Log.UnhandledExceptionLog(args);
        }
    }
}