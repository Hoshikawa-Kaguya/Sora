using Newtonsoft.Json.Linq;
using Sora.Exceptions;
using Sora.Interfaces;
using Sora.OnebotInterface;
using Sora.OnebotModel;
using System;
using System.Data;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Sora.Entities;
using Sora.Entities.Info.InternalDataInfo;
using Websocket.Client;
using YukariToolBox.FormatLog;

namespace Sora.Net
{
    /// <summary>
    /// Sora正向WS链接客户端
    /// </summary>
    public class SoraWebsocketClient : ISoraService
    {
        #region 属性

        /// <summary>
        /// 服务器配置类
        /// </summary>
        private ClientConfig Config { get; set; }

        /// <summary>
        /// WS客户端
        /// </summary>
        private WebsocketClient Client { get; set; }

        /// <summary>
        /// 心跳包检查计时器
        /// </summary>
        private Timer HeartBeatTimer { get; set; }

        /// <summary>
        /// 事件接口
        /// </summary>
        public EventInterface Event { get; }

        /// <summary>
        /// 服务器连接管理器
        /// </summary>
        public ConnectionManager ConnManager { get; }

        #endregion

        #region 私有字段

        /// <summary>
        /// 客户端已准备启动标识
        /// </summary>
        private readonly bool _clientReady;

        /// <summary>
        /// 客户端ID
        /// </summary>
        private readonly Guid _clientId = Guid.NewGuid();

        #endregion

        #region 构造方法

        /// <summary>
        /// 创建一个正向WS客户端
        /// </summary>
        /// <param name="config">配置文件</param>
        /// <param name="crashAction">发生未处理异常时的回调</param>
        /// <exception cref="DataException">数据初始化错误</exception>
        /// <exception cref="ArgumentNullException">空配置文件错误</exception>
        /// <exception cref="ArgumentOutOfRangeException">参数错误</exception>
        internal SoraWebsocketClient(ClientConfig config, Action<Exception> crashAction = null)
        {
            _clientReady = false;
            Log.Info("Sora", $"Sora WebSocket客户端初始化... [{_clientId}]");
            //写入初始化信息
            if (!StaticVariable.ServiceInfos.TryAdd(_clientId, new ServiceInfo(_clientId, config)))
                throw new DataException("try add service info failed");
            //初始化连接管理器
            ConnManager = new ConnectionManager(config);
            //检查参数
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.Port is 0 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(config.Port), "Port out of range");
            //初始化连接管理器
            ConnManager = new ConnectionManager(config);
            Config      = config;
            //实例化事件接口
            Event = new EventInterface(_clientId, config.EnableSoraCommandManager);
            //构建Client配置
            var factory = new Func<ClientWebSocket>(() =>
                                                    {
                                                        var clientWebSocket = new ClientWebSocket();
                                                        clientWebSocket.Options.SetRequestHeader("Authorization",
                                                            $"Bearer {config.AccessToken}");
                                                        return clientWebSocket;
                                                    });
            //处理连接路径
            var serverPath = string.IsNullOrEmpty(config.UniversalPath)
                ? $"ws://{config.Host}:{config.Port}"
                : $"ws://{config.Host}:{config.Port}/{config.UniversalPath.Trim('/')}/";
            Log.Debug("Sora", $"Onebot服务器地址:{serverPath}");
            Client =
                new WebsocketClient(new Uri(serverPath), factory)
                {
                    ReconnectTimeout      = config.ReconnectTimeOut,
                    ErrorReconnectTimeout = config.ReconnectTimeOut
                };
            //全局异常事件
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                                                          {
                                                              if (crashAction == null)
                                                                  Helper.FriendlyException(args);
                                                              else
                                                                  crashAction(args.ExceptionObject as Exception);
                                                          };
            _clientReady = true;
        }

        #endregion

        #region 客户端启动

        /// <summary>
        /// 启动 Sora 服务
        /// </summary>
        public ValueTask StartService() => StartClient();

        /// <summary>
        /// 启动客户端并自动连接服务器
        /// </summary>
        private async ValueTask StartClient()
        {
            if (!_clientReady) return;
            //消息接收订阅
            Client.MessageReceived.Subscribe(msg => Task.Run(() =>
                                                             {
                                                                 Event
                                                                     .Adapter(JObject.Parse(msg.Text), _clientId);
                                                             }));
            Client.DisconnectionHappened.Subscribe(info => Task.Run(() =>
                                                                    {
                                                                        ConnectionManager.GetLoginUid(_clientId,
                                                                            out var uid);
                                                                        //移除原连接信息
                                                                        if (ConnManager
                                                                            .ConnectionExitis(_clientId)
                                                                        )
                                                                            ConnManager.CloseConnection("Universal",
                                                                                uid, _clientId);

                                                                        if (info.Exception != null)
                                                                            Log.Error("Sora",
                                                                                $"监听服务器时发生错误{Log.ErrorLogBuilder(info.Exception)}");
                                                                        else
                                                                            Log.Info("Sora", "服务器连接被关闭");
                                                                    }));
            Client.ReconnectionHappened.Subscribe(info => Task.Run(() =>
                                                                   {
                                                                       if (info.Type == ReconnectionType.Initial)
                                                                           return;
                                                                       Log.Info("Sora", "服务器已自动重连");
                                                                       ConnManager.OpenConnection("Universal", "0",
                                                                           Client, _clientId, _clientId,
                                                                           Config.ApiTimeOut);
                                                                   }));
            await Client.Start();
            if (!Client.IsRunning || !Client.IsStarted)
            {
                throw new WebSocketClientException("WebSocket client is not running");
            }

            ConnManager.OpenConnection("Universal", "0", Client, _clientId, _clientId, Config.ApiTimeOut);
            Log.Info("Sora", "Sora WebSocket客户端正在运行并已连接至onebot服务器");
            //启动心跳包超时检查计时器
            HeartBeatTimer = new Timer(ConnManager.HeartBeatCheck, null,
                                       Config.HeartBeatTimeOut, Config.HeartBeatTimeOut);
        }

        /// <summary>
        /// GC析构函数
        /// </summary>
        ~SoraWebsocketClient()
        {
            Dispose();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StaticVariable.CleanServiceInfo(_clientId);
            HeartBeatTimer.Dispose();
            Client.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}