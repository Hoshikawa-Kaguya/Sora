using System;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Sora;
using Sora.EventArgs.WSSeverEvent;
using Sora.Model;
using Sora.Tool;

namespace Sora_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new Program().MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception e) //侦测所有未处理的错误
            {
                ConsoleLog.UnhandledExceptionLog(e);
            }
        }

        private async Task MainAsync()
        {
            OnebotWSServer server = new OnebotWSServer(new ServerConfig
            {
                HeartBeatTimeOut = 6
            });
            ConsoleLog.SetLogLevel(LogLevel.Debug);
            server.Start();

            server.OnOpenConnectionAsync += (id, args) =>
                                            {
                                                ConsoleLog.Debug("Sora_Test",$"selfId = {id} type = {args.Role}");
                                                return ValueTask.CompletedTask;
                                            };

            server.OnCloseConnectionAsync += (id, args) =>
                                             {
                                                 ConsoleLog.Debug("Sora_Test",$"selfId = {id} type = {args.Role}");
                                                 return ValueTask.CompletedTask;
                                             };
            await Task.Delay(-1);
        }
    }
}
