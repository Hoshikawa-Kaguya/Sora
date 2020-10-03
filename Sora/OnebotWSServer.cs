using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json.Linq;
using Sora.EventArgs.WSSeverEvent;
using Sora.JsonAdapter;
using Sora.Model;
using Sora.Tool;
using Sora.TypeEnum;

namespace Sora
{
    public class OnebotWSServer : IDisposable
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
        private Timer HeartBeatTimer { get; set; }

        /// <summary>
        /// 事件分发
        /// </summary>
        public EventAdapter Event { get; private set; }

        /// <summary>
        /// 链接信息
        /// </summary>
        private readonly Dictionary<Guid, IWebSocketConnection> ConnectionInfos = new Dictionary<Guid, IWebSocketConnection>();

        /// <summary>
        /// 事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数</typeparam>
        /// <param name="selfId">Bot Id</param>
        /// <param name="eventArgs">事件参数</param>
        /// <returns></returns>
        public delegate ValueTask AsyncCallBackHandler<in TEventArgs>(string selfId, TEventArgs eventArgs)where TEventArgs : System.EventArgs;
        #endregion

        #region 回调事件
        /// <summary>
        /// 心跳包处理回调
        /// </summary>
        public event AsyncCallBackHandler<PongEventArgs> OnPongAsync;
        /// <summary>
        /// 打开连接回调
        /// </summary>
        public event AsyncCallBackHandler<ConnectionEventArgs> OnOpenConnectionAsync;
        /// <summary>
        /// 关闭连接回调
        /// </summary>
        public event AsyncCallBackHandler<ConnectionEventArgs> OnCloseConnectionAsync;
        /// <summary>
        /// 错误回调
        /// </summary>
        public event AsyncCallBackHandler<ErrorEventArgs> OnErrorAsync; 
        #endregion

        #region 构造函数

        /// <summary>
        /// 创建一个反向WS客户端
        /// </summary>
        /// <param name="config">服务器配置</param>
        public OnebotWSServer(ServerConfig config)
        {
            //检查参数
            if(config == null) throw new ArgumentNullException(nameof(config));
            if (config.Port < 0 || config.Port > 65535) throw new ArgumentOutOfRangeException(nameof(config.Port));

            this.Config = config;
            //心跳包超时检查计时器
            this.HeartBeatTimer = new Timer(HeartBeatCheck, null, new TimeSpan(0, 0, 0, config.HeartBeatTimeOut, 0),
                                       new TimeSpan(0, 0, 0, config.HeartBeatTimeOut, 0));
            this.Event = new EventAdapter();

            //禁用原log
            FleckLog.Level = LogLevel.Error;
            this.Server    = new WebSocketServer($"ws://0.0.0.0:{config.Port}");
        }
        #endregion

        #region 服务端启动
        /// <summary>
        /// 启动WS服务端
        /// </summary>
        public void Start()
        {
            Server.Start(socket =>
                         {
                             //接收事件处理
                             //获取请求头数据
                             if (!socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID",
                                                                            out string selfId) ||
                                 !socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role",
                                                                           out string role)){return;}
                             //获取连接类型
                             Enum.TryParse(role, out ConnectionType type);
                             //请求路径检查
                             bool isLost;
                             switch (type)
                             {
                                 case ConnectionType.Universal:
                                     isLost = !socket.ConnectionInfo.Path.Trim('/').Equals(Config.UniversalPath);
                                     break;
                                 case ConnectionType.Event:
                                     isLost = !socket.ConnectionInfo.Path.Trim('/').Equals(Config.EventPath);
                                     break;
                                 case ConnectionType.API:
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
                                                 $"Client lost({socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})");
                                 return;
                             }
                             //心跳包
                             socket.OnPong = async (echo) =>
                                             {
                                                 if (OnPongAsync == null) { return; }
                                                 //心跳包事件处理
                                                 await Task.Run(() =>
                                                                {
                                                                    OnPongAsync(selfId, 
                                                                                    new PongEventArgs(echo,
                                                                                        socket.ConnectionInfo));
                                                                });
                                             };
                             //打开连接
                             socket.OnOpen = async () =>
                                             {
                                                 //获取Token
                                                 if (socket.ConnectionInfo.Headers.TryGetValue("Authorization",out string token))
                                                 {
                                                     //验证Token
                                                     if(!token.Equals(this.Config.AccessToken)) return;
                                                 }
                                                 ConnectionInfos.Add(socket.ConnectionInfo.Id, socket);
                                                 //向客户端发送Ping
                                                 await socket.SendPing(new byte[] { 1, 2, 5 });
                                                 //事件回调
                                                 ConnectionEventArgs connection =
                                                     new ConnectionEventArgs(type, socket.ConnectionInfo);
                                                 if (OnOpenConnectionAsync != null)
                                                 {
                                                     await Task.Run(() =>
                                                                    {
                                                                        OnOpenConnectionAsync(selfId, connection);
                                                                    });
                                                 }
                                                 ConsoleLog.Info("Sora",
                                                                 $"Client connected({socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})");
                                             };
                             //关闭连接
                             socket.OnClose = async () =>
                                              {
                                                  //移除原连接信息
                                                  if (ConnectionInfos.Any(conn => conn.Key == socket.ConnectionInfo.Id))
                                                  {
                                                      ConnectionInfos.Remove(socket.ConnectionInfo.Id);
                                                      if (OnCloseConnectionAsync != null)
                                                      {
                                                          await Task.Run(() =>
                                                                         {
                                                                             OnCloseConnectionAsync(selfId,
                                                                                 new ConnectionEventArgs(type,
                                                                                     socket.ConnectionInfo));
                                                                         });
                                                      }
                                                  }
                                                  ConsoleLog.Info("Sora",
                                                                     $"Closed connection({socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})");
                                              };
                             //上报接收
                             socket.OnMessage = async (message) =>
                                                {
                                                    //处理接收的数据
                                                    // ReSharper disable once SimplifyLinqExpressionUseAll
                                                    if (!ConnectionInfos.Any(conn => conn.Key ==
                                                                                 socket.ConnectionInfo.Id)) return;
                                                    try
                                                    {
                                                        //进入事件处理和分发
                                                        await Task.Run(() =>
                                                                       {
                                                                           this.Event.Adapter(JObject.Parse(message),
                                                                               socket.ConnectionInfo.Id);
                                                                       });
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine(e);
                                                        if (OnErrorAsync != null)
                                                        {
                                                            //错误事件回调
                                                            await Task.Run(() =>
                                                                           {
                                                                               OnErrorAsync(selfId,
                                                                                   new ErrorEventArgs(e, socket.ConnectionInfo));
                                                                           });
                                                        }
                                                    }
                                                };
                         });
            ConsoleLog.Info("Sora",$"Server running at 0.0.0.0:{Config.Port}");
        }
        ~OnebotWSServer()
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
            ConsoleLog.Debug("Sora","Heartbeat check");
            foreach (KeyValuePair<Guid, long> conn in MetaEventAdapter.HeartBeatList)
            {
                ConsoleLog.Debug("Sora",$"Connection check | {conn.Key} | {Utils.GetNowTimeStamp() - conn.Value}");
                //检查超时的连接
                if (Utils.GetNowTimeStamp() - conn.Value > Config.HeartBeatTimeOut)
                {
                    //关闭超时的连接
                    IWebSocketConnection lostConnection = ConnectionInfos[conn.Key];
                    lostConnection.Close();
                    ConsoleLog.Error("Sora",
                                     $"Client {lostConnection.ConnectionInfo.ClientIpAddress}:{lostConnection.ConnectionInfo.ClientPort} lost (hreatbeat time out)");
                    ConnectionInfos.Remove(conn.Key);
                    MetaEventAdapter.HeartBeatList.Remove(conn.Key);
                }
            }
        }
        #endregion
    }
}
