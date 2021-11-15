using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Sora.Interfaces;
using Sora.Net;
using Sora.Net.Config;
using YukariToolBox.LightLog;

namespace Sora;

/// <summary>
/// Sora 实例工厂
/// </summary>
public static class SoraServiceFactory
{
    private static bool _haveStartupLog;

    /// <summary>
    /// 创建一个Sora服务
    /// </summary>
    /// <param name="config">配置文件</param>
    /// <param name="crashAction">发生未处理异常时的回调</param>
    /// <exception cref="DataException">数据初始化错误</exception>
    /// <exception cref="ArgumentNullException">空配置文件错误</exception>
    /// <exception cref="ArgumentOutOfRangeException">参数错误</exception>
    /// <exception cref="ArgumentException">配置文件类型错误</exception>
    public static ISoraService CreateService(ISoraConfig config, Action<Exception> crashAction = null)
    {
        //如果已经启动过初始化则不显示初始化log
        if (!_haveStartupLog)
        {
            Log.Info("Sora", $"框架版本:{StaticVariable.Version}");
            Log.Debug("Sora", "开发交流群：1081190562");
            Log.Debug("System", Environment.OSVersion.ToString());
            Log.Debug("Runtime", Environment.Version.ToString());
            Log.Info("OnebotProtocolVersion", "11");
            _haveStartupLog =  true;
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
        return configList.Select(soraConfig => CreateService(soraConfig, crashAction)).ToList();
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
        var createdService = new List<ISoraService>()
        {
            CreateService(config, crashAction)
        };

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
    /// <param name="serviceList">多服务列表</param>
    public static async ValueTask StartMultiService(this IEnumerable<ISoraService> serviceList)
    {
        foreach (var soraService in serviceList) await soraService.StartService();
    }
}