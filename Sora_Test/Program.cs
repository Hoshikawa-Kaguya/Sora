using Sora;
using Sora.Entities.Segment;
using Sora.Enumeration;
using Sora.Net.Config;
using System.Threading.Tasks;
using YukariToolBox.LightLog;

//设置log等级
Log.LogConfiguration
   .EnableConsoleOutput()
   .SetLogLevel(LogLevel.Debug);

//实例化Sora服务
var service = SoraServiceFactory.CreateService(new ServerConfig());

#region 事件处理

//连接事件
service.ConnManager.OnOpenConnectionAsync += (connectionId, eventArgs) =>
                                             {
                                                 Log.Debug("Sora_Test|OnOpenConnectionAsync",
                                                           $"connectionId = {connectionId} type = {eventArgs.Role}");
                                                 return ValueTask.CompletedTask;
                                             };
//连接关闭事件
service.ConnManager.OnCloseConnectionAsync += (connectionId, eventArgs) =>
                                              {
                                                  Log.Debug("Sora_Test|OnCloseConnectionAsync",
                                                            $"uid = {eventArgs.SelfId} connectionId = {connectionId} type = {eventArgs.Role}");
                                                  return ValueTask.CompletedTask;
                                              };
//连接成功元事件
service.Event.OnClientConnect += (_, eventArgs) =>
                                 {
                                     Log.Debug("Sora_Test|OnClientConnect",
                                               $"uid = {eventArgs.LoginUid}");
                                     return ValueTask.CompletedTask;
                                 };

//群聊消息事件
service.Event.OnGroupMessage += async (_, eventArgs) => { await eventArgs.Reply("好耶"); };
service.Event.OnSelfMessage += (_, eventArgs) =>
                               {
                                   Log.Info("test", $"self msg {eventArgs.Message.MessageId}");
                                   return ValueTask.CompletedTask;
                               };
//私聊消息事件
service.Event.OnPrivateMessage += async (_, eventArgs) => { await eventArgs.Reply("好耶"); };
//动态向管理器注册指令
service.Event.CommandManager.RegisterGroupCommand(new[] {"2"},
                                                  async eventArgs =>
                                                  {
                                                      await eventArgs.Reply(SoraSegment.At(4564) + 2133.ToAt() +
                                                                            "fuck");
                                                      eventArgs.IsContinueEventChain = false;
                                                  }, MatchType.Full);

#endregion

//启动服务并捕捉错误
await service.StartService();
//.RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));
await Task.Delay(-1);