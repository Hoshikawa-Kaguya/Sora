using System;
using System.Threading.Tasks;
using Sora.Extensions;
using Sora.Server;
using YukariToolBox.Console;

namespace Sora_Test
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            ConsoleLog.SetLogLevel(LogLevel.Debug);
            ConsoleLog.Debug("dotnet version",Environment.Version);

            SoraWSServer server = new SoraWSServer(new ServerConfig{Port = 8080});

            //服务器连接事件
            server.ConnManager.OnOpenConnectionAsync += (connectionInfo, eventArgs) =>
                                                        {
                                                            ConsoleLog.Debug("Sora_Test",
                                                                             $"connectionId = {connectionInfo.Id} type = {eventArgs.Role}");
                                                            return ValueTask.CompletedTask;
                                                        };
            //服务器连接关闭事件
            server.ConnManager.OnCloseConnectionAsync += (connectionInfo, eventArgs) =>
                                                         {
                                                             ConsoleLog.Debug("Sora_Test",
                                                                              $"connectionId = {connectionInfo.Id} type = {eventArgs.Role}");
                                                             return ValueTask.CompletedTask;
                                                         };
            //服务器心跳包超时事件
            server.ConnManager.OnHeartBeatTimeOut += (connectionInfo, eventArgs) =>
                                                     {
                                                         ConsoleLog.Debug("Sora_Test",
                                                                          $"Get heart beat time out from[{connectionInfo.Id}] uid[{eventArgs.SelfId}]");
                                                         return ValueTask.CompletedTask;
                                                     };
            //群聊消息事件
            server.Event.OnGroupMessage += async (sender, eventArgs) =>
                                           {
                                               var msg = await eventArgs.SoraApi.GetOnlineClients(false);
                                           };
            //私聊消息事件
            server.Event.OnPrivateMessage += async (sender, eventArgs) =>
                                               {
                                                   await eventArgs.Sender.SendPrivateMessage("好耶");
                                               };

            await server.StartServer().RunCatch();
        }
    }
}
