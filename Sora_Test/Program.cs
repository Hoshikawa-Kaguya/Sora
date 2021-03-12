using DllTest;
using Sora.Interfaces;
using Sora.Net;
using Sora.OnebotModel;
using System.Reflection;
using System.Threading.Tasks;
using YukariToolBox.Extensions;
using YukariToolBox.FormatLog;

//设置log等级
Log.SetLogLevel(LogLevel.Debug);

//实例化Sora服务
ISoraService service = new SoraWebsocketClient(new ClientConfig());
service.Event.CommandManager.MappingCommands(Assembly.GetAssembly(typeof(MyCommand)));

#region 事件处理

//连接事件
service.ConnManager.OnOpenConnectionAsync += (connectionInfo, eventArgs) =>
                                             {
                                                 Log.Debug("Sora_Test",
                                                           $"connectionId = {connectionInfo} type = {eventArgs.Role}");
                                                 return ValueTask.CompletedTask;
                                             };
//连接关闭事件
service.ConnManager.OnCloseConnectionAsync += (connectionInfo, eventArgs) =>
                                              {
                                                  Log.Debug("Sora_Test",
                                                            $"connectionId = {connectionInfo} type = {eventArgs.Role}");
                                                  return ValueTask.CompletedTask;
                                              };
//心跳包超时事件
service.ConnManager.OnHeartBeatTimeOut += (connectionInfo, eventArgs) =>
                                          {
                                              Log.Debug("Sora_Test",
                                                        $"Get heart beat time out from[{connectionInfo}] uid[{eventArgs.SelfId}]");
                                              return ValueTask.CompletedTask;
                                          };
//群聊消息事件
service.Event.OnGroupMessage += async (msgType, eventArgs) => { await eventArgs.SourceGroup.SendGroupMessage("好耶"); };
service.Event.OnSelfMessage += (type, eventArgs) =>
                               {
                                   Log.Info("test", $"self msg {eventArgs.Message.MessageId}");
                                   return ValueTask.CompletedTask;
                               };
//私聊消息事件
service.Event.OnPrivateMessage += async (msgType, eventArgs) => { await eventArgs.Sender.SendPrivateMessage("好耶"); };

#endregion

//启动服务并捕捉错误
await service.StartService().RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));
await Task.Delay(-1);