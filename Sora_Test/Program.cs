using System;
using System.Threading.Tasks;
using Fleck;
using Sora.Server;
using Sora.Tool;

namespace Sora_Test
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                ConsoleLog.SetLogLevel(LogLevel.Debug);
                SoraWSServer server = new SoraWSServer(new ServerConfig());
                server.OnOpenConnectionAsync += (id, eventArgs) =>
                                                {
                                                    ConsoleLog.Debug("Sora_Test",$"selfId = {id} type = {eventArgs.Role}");
                                                    return ValueTask.CompletedTask;
                                                };
            
                server.OnCloseConnectionAsync += (id, eventArgs) =>
                                                 {
                                                     ConsoleLog.Debug("Sora_Test",$"selfId = {id} type = {eventArgs.Role}");
                                                     return ValueTask.CompletedTask;
                                                 };
                server.Event.OnGroupMessage += async (sender, eventArgs) =>
                                               {
                                                   //最简单的复读（x
                                                   await eventArgs.Repeat();
                                               };
                await server.StartServerAsync();
            }
            catch (Exception e) //侦测所有未处理的错误
            {
                ConsoleLog.Fatal("unknown error",ConsoleLog.ErrorLogBuilder(e));
            }
        }
    }
}
