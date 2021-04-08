using System;
using Sora.Interfaces;
using Sora.OnebotModel;
using YukariToolBox.FormatLog;

namespace Sora.Net
{
    /// <summary>
    /// Sora 实例工厂
    /// </summary>
    public class SoraServiceFactory
    {
        private static bool _haveStartupLog;

        /// <summary>
        /// 创建 Sora 服务实例
        /// </summary>
        /// <param name="config">服务器配置</param>
        /// <param name="crashAction">发生未处理异常时的回调</param>
        /// <returns>Sora 服务实例</returns>
        public static ISoraService CreateService(ISoraConfig config, Action<Exception> crashAction = null)
        {
            if (!_haveStartupLog)
            {
                Log.Info("Sora", $"Sora 框架版本:1.0.0-rc.5"); //{Assembly.GetExecutingAssembly().GetName().Version}");
                Log.Debug("Sora", "开发交流群：1081190562");
                Log.Debug("System", Environment.OSVersion);
                _haveStartupLog = true;
            }

            return config switch
            {
                ClientConfig s1 => new SoraWebsocketClient(s1, crashAction),
                ServerConfig s2 => new SoraWebsocketServer(s2, crashAction),
                _ => throw new ArgumentException("接收到了不认识的 Sora 配置对象。")
            };
        }
    }
}