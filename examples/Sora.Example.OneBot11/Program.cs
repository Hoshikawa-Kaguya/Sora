using Microsoft.Extensions.Logging;
using Sora;
using Sora.Adapter.OneBot11;
using Sora.Example.OneBot11;

// 创建服务
SoraService service = SoraServiceFactory.Instance.CreateOneBot11Service(
    new OneBot11Config
        {
            Mode              = ConnectionMode.ForwardWebSocket,
            Host              = "127.0.0.1",
            Port              = 3001,
            AccessToken       = "",
            HeartbeatInterval = TimeSpan.FromSeconds(5),
            MinimumLogLevel   = LogLevel.Information
        });

ILogger logger = SoraLogger.CreateLogger("OneBot11Bot");

// 连接成功
service.Events.OnConnected += async e =>
{
    logger.LogInformation("[已连接] 连接ID: {ConnectionId}", e.ConnectionId);
    await ValueTask.CompletedTask;
};

// 连接断开
service.Events.OnDisconnected += async e =>
{
    logger.LogInformation("[已断开] 原因: {Reason}", e.Reason);
    await ValueTask.CompletedTask;
};

// 消息接收
service.Events.OnMessageReceived += async e => { await Helpers.SendReplyAsync(e, new MessageBody("干嘛")); };

// 注册指令
service.Commands.ScanAssembly(typeof(Program).Assembly);

logger.LogInformation("Link start...");
await service.StartAsync();

await Task.Delay(Timeout.Infinite);