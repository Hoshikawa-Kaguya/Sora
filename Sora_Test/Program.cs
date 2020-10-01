using System;
using System.Threading;
using System.Threading.Tasks;
using Sora;

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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }
        }

        private async Task MainAsync()
        {
            OnebotWSServer server = new OnebotWSServer(port:8080,
                                                       universalPath:"ws");
            server.Start();

            server.OnOpenConnectionAsync += (id, args) =>
                                            {
                                                Console.WriteLine($"selfId = {id}\ntype = {args.Role}\nclient path = {args.ConnectionInfo.ClientIpAddress}{args.ConnectionInfo.Path}");
                                                return ValueTask.CompletedTask;
                                            };

            
            await Task.Delay(-1);
        }
    }
}
