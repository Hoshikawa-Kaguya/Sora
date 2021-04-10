using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        /// <summary>
        /// 连续创建多个 Sora 服务实例
        /// </summary>
        /// <param name="configList">服务配置列表</param>
        /// <param name="crashAction">发生未处理异常时的统一回调</param>
        /// <returns>Sora 服务实例列表</returns>
        public static List<ISoraService> CreateMultiService(IEnumerable<ISoraConfig> configList,
                                                            Action<Exception> crashAction = null)
        {
            List<ISoraService> createdService = new();
            foreach (ISoraConfig soraConfig in configList)
            {
                createdService.Add(CreateService(soraConfig, crashAction));
            }

            return createdService;
        }

        /// <summary>
        /// 连续创建多个 Sora 服务实例
        /// </summary>
        /// <param name="config">服务配置</param>
        /// <param name="crashAction">发生未处理异常时的统一回调</param>
        /// <returns>Sora 服务实例列表</returns>
        public static List<ISoraService> CreateMultiService(ISoraConfig config,
                                                            Action<Exception> crashAction = null)
        {
            List<ISoraService> createdService = new();
            createdService.Add(CreateService(config, crashAction));

            return createdService;
        }
    }

    /// <summary>
    /// SoraServiceFactoryExtension
    /// </summary>
    public static class SoraServiceFactoryExtension
    {
        /// <summary>
        /// 启动多个服务
        /// </summary>
        /// <param name="serviceList"></param>
        public static async ValueTask StartMultiService(this IEnumerable<ISoraService> serviceList)
        {
            foreach (ISoraService soraService in serviceList)
            {
                await soraService.StartService();
            }
        }
    }
}