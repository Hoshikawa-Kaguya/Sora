using System;
using System.Threading.Tasks;
using Fleck;
using Sora;
using Sora.Model;
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
                OnebotWSServer server = new OnebotWSServer(new ServerConfig());
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
                await Task.Delay(-1);
            }
            catch (Exception e) //侦测所有未处理的错误
            {
                ConsoleLog.UnhandledExceptionLog(e);
            }
        }
    }
}
