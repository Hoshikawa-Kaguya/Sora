using System.Reflection;
using System.Threading.Tasks;
using Sora.Net;
using Sora.OnebotModel;
using YukariToolBox.FormatLog;
using YukariToolBox.Extensions;

//设置log等级
Log.SetLogLevel(LogLevel.Debug);

//实例化客户端
SoraWebsocketClient client = new(new ClientConfig());
client.Event.CommandManager.MappingCommands(Assembly.GetEntryAssembly());
#region 服务器事件处理
//客户端连接事件
client.ConnManager.OnOpenConnectionAsync += (connectionInfo, eventArgs) =>
                                            {
                                                Log.Debug("Sora_Test",
                                                                 $"connectionId = {connectionInfo} type = {eventArgs.Role}");
                                                return ValueTask.CompletedTask;
                                            };
//客户端连接关闭事件
client.ConnManager.OnCloseConnectionAsync += (connectionInfo, eventArgs) =>
                                             {
                                                 Log.Debug("Sora_Test",
                                                                  $"connectionId = {connectionInfo} type = {eventArgs.Role}");
                                                 return ValueTask.CompletedTask;
                                             };
//客户端心跳包超时事件
client.ConnManager.OnHeartBeatTimeOut += (connectionInfo, eventArgs) =>
                                         {
                                             Log.Debug("Sora_Test",
                                                              $"Get heart beat time out from[{connectionInfo}] uid[{eventArgs.SelfId}]");
                                             return ValueTask.CompletedTask;
                                         };
//群聊消息事件
client.Event.OnGroupMessage += async (msgType, eventArgs) =>
                               {
                                   await eventArgs.SourceGroup.SendGroupMessage("好耶");
                               };
client.Event.OnSelfMessage += (type, eventArgs) =>
                              {
                                  Log.Info("test", $"self msg {eventArgs.Message.MessageId}");
                                  return ValueTask.CompletedTask;
                              };
//私聊消息事件
client.Event.OnPrivateMessage += async (msgType, eventArgs) =>
                                 {
                                     await eventArgs.Sender.SendPrivateMessage("好耶");
                                 };

#endregion

//启动客户端并捕捉错误
await client.StartClient().RunCatch(e => Log.Error("Server Error", Log.ErrorLogBuilder(e)));

await Task.Delay(-1);