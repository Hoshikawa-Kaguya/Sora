using System;
using System.Collections.Generic;
using System.Linq;

using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json.Linq;
using Sora.EventArgs.WSSeverEvent;
using Sora.ServerInterface;
using Sora.Tool;

namespace Sora
{
    public sealed class SoraWSServer : IDisposable
    {
        #region 属性
        /// <summary>
        /// 服务器配置类
        /// </summary>
        private ServerConfig Config { get; set; }

        /// <summary>
        /// WS服务器
        /// </summary>
        private WebSocketServer Server { get; set; }

        /// <summary>
        /// 心跳包检查计时器
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private Timer HeartBeatTimer { get; set; }

        /// <summary>
        /// 事件接口
        /// </summary>
        public EventInterface Event { get; set; }

        private readonly bool ready_flag = false;

        /// <summary>
        /// 链接信息
        /// </summary>
        internal static readonly Dictionary<Guid, IWebSocketConnection> ConnectionInfos;

        /// <summary>
        /// 服务器事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数</typeparam>
        /// <param name="selfId">Bot Id</param>
        /// <param name="eventArgs">事件参数</param>
        /// <returns></returns>
        public delegate ValueTask ServerAsyncCallBackHandler<in TEventArgs>(string selfId, TEventArgs eventArgs)where TEventArgs : System.EventArgs;
        #endregion

        #region 回调事件
        /// <summary>
        /// 心跳包处理回调
        /// </summary>
        public event ServerAsyncCallBackHandler<PongEventArgs> OnPongAsync;
        /// <summary>
        /// 打开连接回调
        /// </summary>
        public event ServerAsyncCallBackHandler<ConnectionEventArgs> OnOpenConnectionAsync;
        /// <summary>
        /// 关闭连接回调
        /// </summary>
        public event ServerAsyncCallBackHandler<ConnectionEventArgs> OnCloseConnectionAsync;
        #endregion

        #region 构造函数
        /// <summary>
        /// 静态构造函数
        /// 用于初始化静态处理资源
        /// </summary>
        static SoraWSServer()
        {
            //初始化连接表
            ConnectionInfos = new Dictionary<Guid, IWebSocketConnection>();
            //全局异常事件
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                                                          {
                                                              ConsoleLog.UnhandledExceptionLog(args);
                                                          };
        }

        /// <summary>
        /// 使用端口配置创建一个反向WS服务端
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static SoraWSServer CreateDefault(int port = 9200)
        {
            return new SoraWSServer(port);
        }

        /// <summary>
        /// 使用默认配置创建一个反向WS服务端
        /// </summary>
        /// <param name="port">服务端端口</param>
        public SoraWSServer(int port = 9200) : this(new ServerConfig { Port = port })
        {

        }

        /// <summary>
        /// 创建一个反向WS服务端
        /// </summary>
        /// <param name="config">服务器配置</param>
        public SoraWSServer(ServerConfig config)
        {
            ConsoleLog.Info("Sora",$"Sora WebSocket服务器初始化...");
            //检查参数
            if(config == null) throw new ArgumentNullException(nameof(config));
            if (config.Port < 0 || config.Port > 65535) throw new ArgumentOutOfRangeException(nameof(config.Port));

            this.Config = config;
            //心跳包超时检查计时器
            this.HeartBeatTimer = new Timer(HeartBeatCheck, null, new TimeSpan(0, 0, 0, config.HeartBeatTimeOut, 0),
                                       new TimeSpan(0, 0, 0, config.HeartBeatTimeOut, 0));
            //API超时
            ApiInterface.TimeOut = config.ApiTimeOut;
            //实例化事件接口
            this.Event = new EventInterface();
            //禁用原log
            FleckLog.Level = (LogLevel)4;
            this.Server    = new WebSocketServer($"ws://{config.Location}:{config.Port}")
            {
                //出错后进行重启
                RestartAfterListenError = true
            };
            if (PortInUse(config.Port))
            {
                ConsoleLog.Fatal("Sora", $"端口{config.Port}已被占用，请更换其他端口");
                ConsoleLog.Warning("Sora", "将在5s后自动退出");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }
            else
                ready_flag = true;
        }
        #endregion

        #region 服务端启动
        /// <summary>
        /// 启动WS服务端
        /// </summary>
        public async Task StartAsync()
        {
            if (!ready_flag) await Task.CompletedTask;
            Server.Start(socket =>
                         {
                             //接收事件处理
                             //获取请求头数据
                             if (!socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID",
                                                                            out string selfId) ||       //bot UID
                                 !socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role",
                                                                            out string role)){return;}   //Client Type

                             //请求路径检查
                             bool isLost;
                             switch (role)
                             {
                                 case "Universal":
                                     isLost = !socket.ConnectionInfo.Path.Trim('/').Equals(Config.UniversalPath);
                                     break;
                                 case "Event":
                                     isLost = !socket.ConnectionInfo.Path.Trim('/').Equals(Config.EventPath);
                                     break;
                                 case "API":
                                     isLost = !socket.ConnectionInfo.Path.Trim('/').Equals(Config.ApiPath);
                                     break;
                                 default:
                                     isLost = false;
                                     break;
                             }
                             if (isLost)
                             {
                                 socket.Close();
                                 ConsoleLog.Warning("Sora",
                                                    $"关闭与未知客户端的连接[{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]，请检查是否设置正确的监听地址");
                                 return;
                             }
                             //心跳包
                             socket.OnPong = (echo) =>
                                             {
                                                 if (OnPongAsync == null) { return; }
                                                 //心跳包事件处理
                                                 Task.Run( async () =>
                                                          {
                                                              await OnPongAsync(selfId,
                                                                          new PongEventArgs(echo,
                                                                              socket.ConnectionInfo));
                                                          });
                                             };
                             //打开连接
                             socket.OnOpen = () =>
                                             {
                                                 //获取Token
                                                 if (socket.ConnectionInfo.Headers.TryGetValue("Authorization",out string token))
                                                 {
                                                     //验证Token
                                                     if(!token.Equals(this.Config.AccessToken)) return;
                                                 }
                                                 ConnectionInfos.Add(socket.ConnectionInfo.Id, socket);
                                                 //向客户端发送Ping
                                                 socket.SendPing(new byte[] { 1, 2, 5 });
                                                 //事件回调
                                                 ConnectionEventArgs connection =
                                                     new ConnectionEventArgs(role, socket.ConnectionInfo);
                                                 if (OnOpenConnectionAsync == null) return;
                                                 Task.Run( async () =>
                                                           {
                                                               await OnOpenConnectionAsync(selfId, connection);
                                                           });
                                                 ConsoleLog.Info("Sora",
                                                                 $"已连接客户端[{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                                             };
                             //关闭连接
                             socket.OnClose = () =>
                                              {
                                                  //移除原连接信息
                                                  if (ConnectionInfos.Any(conn => conn.Key == socket.ConnectionInfo.Id))
                                                  {
                                                      ConnectionInfos.Remove(socket.ConnectionInfo.Id);
                                                      if (OnCloseConnectionAsync == null) return;
                                                      Task.Run( async () =>
                                                                {
                                                                    await OnCloseConnectionAsync(selfId,
                                                                        new ConnectionEventArgs(role,
                                                                            socket.ConnectionInfo));
                                                                });
                                                  }
                                                  ConsoleLog.Info("Sora",
                                                                  $"客户端连接被关闭[{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                                              };
                             //上报接收
                             socket.OnMessage = (message) =>
                                                {
                                                    //处理接收的数据
                                                    // ReSharper disable once SimplifyLinqExpressionUseAll
                                                    if (!ConnectionInfos.Any(conn => conn.Key ==
                                                                                 socket.ConnectionInfo.Id)) return;
                                                    //进入事件处理和分发
                                                    Task.Run(() =>
                                                             {
                                                                 this.Event
                                                                     .Adapter(JObject.Parse(message),
                                                                              socket.ConnectionInfo.Id);
                                                             });
                                                };
                         });
            ConsoleLog.Info("Sora",$"Sora WebSocket服务器正在运行[{Config.Location}:{Config.Port}]");
            ConsoleLog.Info("Sora",$"Sora 服务端框架版本:{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            await Task.Delay(-1);
        }
        ~SoraWSServer()
        {
            Dispose();
        }
        public void Dispose()
        {
            Server?.Dispose();
            ConnectionInfos.Clear();
        }
        #endregion

        #region 服务器事件处理方法
        /// <summary>
        /// 心跳包超时检查
        /// </summary>
        private void HeartBeatCheck(object msg)
        {
            if(ConnectionInfos.Count == 0) return;
            foreach (KeyValuePair<Guid, long> conn in EventInterface.HeartBeatList)
            {
                //ConsoleLog.Debug("Sora",$"Connection check | {conn.Key} | {Utils.GetNowTimeStamp() - conn.Value}");
                //检查超时的连接
                if (Utils.GetNowTimeStamp() - conn.Value > Config.HeartBeatTimeOut)
                {
                    try
                    {
                        //关闭超时的连接
                        ConnectionInfos.TryGetValue(conn.Key, out IWebSocketConnection lostConnection);
                        if (lostConnection == null)
                        {
                            ConsoleLog.Error("Sora","检测到不存在的客户端");
                            EventInterface.HeartBeatList.Remove(conn.Key);
                            return;
                        }
                        lostConnection.Close();
                        ConsoleLog.Error("Sora",
                                         $"与Onebot客户端[{lostConnection.ConnectionInfo.ClientIpAddress}:{lostConnection.ConnectionInfo.ClientPort}]失去链接(心跳包超时)");
                        ConnectionInfos.Remove(conn.Key);
                        EventInterface.HeartBeatList.Remove(conn.Key);
                    }
                    catch (Exception e)
                    {
                        ConsoleLog.Error("Sora","检查心跳包时发生错误");
                        ConsoleLog.Error("Sora",ConsoleLog.ErrorLogBuilder(e));
                        EventInterface.HeartBeatList.Remove(conn.Key);
                        ConnectionInfos.Remove(conn.Key);
                    }
                }
            }
        }

        /// <summary>
        /// 检查端口占用
        /// </summary>
        /// <param name="port">端口号</param>
        private static bool PortInUse(int port) =>
            IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
                              .Any(ipEndPoint => ipEndPoint.Port == port);
        #endregion
    }
}
