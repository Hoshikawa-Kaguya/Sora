using System;
using System.Threading;
using System.Threading.Tasks;
using Sora;
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
            OnebotWSServer server = new OnebotWSServer(new ServerConfig());
            server.Start();

            server.OnOpenConnectionAsync += (id, args) =>
                                            {
                                                Console.WriteLine($"selfId = {id}\ntype = {args.Role}\nclient path = {args.ConnectionInfo.ClientIpAddress}{args.ConnectionInfo.Path}");
                                                return ValueTask.CompletedTask;
                                            };

            server.OnCloseConnectionAsync += (id, args) =>
                                             {
                                                 Console.WriteLine($"selfId = {id}\ntype = {args.Role}\nclient path = {args.ConnectionInfo.ClientIpAddress}{args.ConnectionInfo.Path}");
                                                 return ValueTask.CompletedTask;
                                             };
            
            await Task.Delay(-1);
        }
    }
}
