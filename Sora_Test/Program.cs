using System.Linq;
using System.Threading.Tasks;
using Sora;
using Sora.Interfaces;
using Sora.Net.Config;
using YukariToolBox.LightLog;

//设置log等级
Log.LogConfiguration
   .EnableConsoleOutput()
   .SetLogLevel(LogLevel.Debug);

//实例化Sora服务
ISoraService service = SoraServiceFactory.CreateService(new ServerConfig
{
    EnableSocketMessage   = false,
    ThrowCommandException = false
});

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
service.Event.OnGroupMessage += async (_, eventArgs) =>
{
    await eventArgs.Reply($"{eventArgs.Message.MessageBody.First().DataType}");
};
service.Event.OnSelfGroupMessage += (_, eventArgs) =>
{
    Log.Warning("test", $"self group msg {eventArgs.Message.MessageId}[{eventArgs.IsSelfMessage}]");
    return ValueTask.CompletedTask;
};
//私聊消息事件
service.Event.OnPrivateMessage += async (_, eventArgs) => { await eventArgs.Reply("好耶"); };
service.Event.OnSelfPrivateMessage += (_, eventArgs) =>
{
    Log.Warning("test", $"self private msg {eventArgs.Message.MessageId}[{eventArgs.IsSelfMessage}]");
    return ValueTask.CompletedTask;
};
//动态向管理器注册指令
service.Event.CommandManager.RegisterGroupDynamicCommand(
    new[] {"2"},
    async eventArgs =>
    {
        await eventArgs.Reply("shit");
        eventArgs.IsContinueEventChain = false;
    });

#endregion

//启动服务并捕捉错误
await service.StartService();
//.RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));

await Task.Delay(-1);