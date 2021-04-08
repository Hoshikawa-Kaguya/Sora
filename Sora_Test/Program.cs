using Sora.Net;
using Sora.OnebotModel;
using System.Threading.Tasks;
using YukariToolBox.Extensions;
using YukariToolBox.FormatLog;

//设置log等级
Log.SetLogLevel(LogLevel.Debug);

//实例化Sora服务
var service = SoraServiceFactory.CreateService(new ServerConfig());

#region 事件处理

//连接事件
service.ConnManager.OnOpenConnectionAsync += (connectionInfo, eventArgs) =>
                                             {
                                                 Log.Debug("Sora_Test|OnOpenConnectionAsync",
                                                           $"connectionId = {connectionInfo} type = {eventArgs.Role}");
                                                 return ValueTask.CompletedTask;
                                             };
//连接关闭事件
service.ConnManager.OnCloseConnectionAsync += (connectionInfo, eventArgs) =>
                                              {
                                                  Log.Debug("Sora_Test|OnCloseConnectionAsync",
                                                            $"uid = {eventArgs.SelfId} connectionId = {connectionInfo} type = {eventArgs.Role}");
                                                  return ValueTask.CompletedTask;
                                              };
//心跳包超时事件
service.ConnManager.OnHeartBeatTimeOut += (connectionInfo, eventArgs) =>
                                          {
                                              Log.Debug("Sora_Test|OnHeartBeatTimeOut",
                                                        $"Get heart beat time out from[{connectionInfo}] uid[{eventArgs.SelfId}]");
                                              return ValueTask.CompletedTask;
                                          };
//连接成功元事件
service.Event.OnClientConnect += (type, eventArgs) =>
                                 {
                                     Log.Debug("Sora_Test|OnClientConnect",
                                               $"uid = {eventArgs.LoginUid}");
                                     return ValueTask.CompletedTask;
                                 };

//群聊消息事件
service.Event.OnGroupMessage += async (msgType, eventArgs) => { await eventArgs.Reply("好耶"); };
service.Event.OnSelfMessage += (type, eventArgs) =>
                               {
                                   Log.Info("Sora_Test|OnSelfMessage", $"self msg {eventArgs.Message.MessageId}");
                                   return ValueTask.CompletedTask;
                               };
//私聊消息事件
service.Event.OnPrivateMessage += async (msgType, eventArgs) => { await eventArgs.Sender.SendPrivateMessage("好耶"); };

#endregion

//启动服务并捕捉错误
await service.StartService().RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));
await Task.Delay(-1);