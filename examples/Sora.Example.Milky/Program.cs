using Microsoft.Extensions.Logging;
using Sora;
using Sora.Adapter.Milky;
using Sora.Entities.Segments;
using Sora.Example.Milky;
using Sora.Example.Milky.Commands;
using Sora.Example.Milky.Filters;

SoraLogger.Configure(SoraLogger.CreateDefaultLoggerConfiguration(LogLevel.Debug));

// 创建服务
SoraService service = SoraServiceFactory.Instance.CreateMilkyService(
    new MilkyConfig
    {
        Host           = "127.0.0.1",
        Port           = 3010,
        Prefix         = "milky",
        AccessToken    = "test",
        EventTransport = EventTransport.WebSocket
    });

ILogger logger = SoraLogger.CreateLogger("MilkyBot");

// 注册事件过滤器（在 StartAsync 之前）
// 命令过滤器请直接以 attribute 形式贴在 [Command] 方法或 [CommandGroup] 类上（见 BasicCommands / CooldownAttribute）。
service.UseEventPreFilter(new LoggingPreFilter(logger));
service.UseEventPostFilter(new MetricsPostFilter(logger));

//事件处理

//连接事件
service.Events.OnConnected += async e =>
{
    logger.LogInformation("已连接 ID: {ConnectionId}", e.ConnectionId);
    await ValueTask.CompletedTask;
};

// 连接断开
service.Events.OnDisconnected += async e =>
{
    logger.LogInformation("已断开 {Reason}", e.Reason);
    await ValueTask.CompletedTask;
};

//消息接收
service.Events.OnMessageReceived += async e =>
{
    ForwardSegment t = SegmentBuilder.Forward(
    [
        new ForwardedMessageNode
        {
            UserId     = 114514,
            SenderName = "YBB",
            Time       = DateTime.Now - TimeSpan.FromHours(1),
            Segments   = "shit"
        }
    ]);
    await e.Api.SendGroupMessageAsync(e.Group.GroupId, t);
    await Helpers.SendReplyAsync(e, new MessageBody("干嘛"));
};

//群成员加入
service.Events.OnMemberJoined += async e =>
{
    logger.LogInformation("群[{GroupId}]新成员:{UserId}", e.GroupId, e.UserId);
    ApiResult<GroupMemberInfo> member = await e.Api.GetGroupMemberInfoAsync(e.GroupId, e.UserId);
    MessageBody welcome = new MessageBody()
                          .AddMention(e.UserId)
                          .AddText($"你是一个一个一个{member.Data?.Nickname}啊啊啊");
    await e.Api.SendGroupMessageAsync(e.GroupId, welcome);
};

// 指令注册
service.Commands.ScanAssembly(typeof(Program).Assembly);
DynamicCommandExamples examples = new();
examples.Register(service);

logger.LogInformation("Link start...");
await service.StartAsync();

await Task.Delay(Timeout.Infinite);