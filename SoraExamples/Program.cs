using System.Threading.Tasks;
using Sora;
using Sora.Tool;

namespace SoraExamples
{
    static class Program
    {
        static async Task Main(string[] args)
        {
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
            await Task.Delay(-1);
        }
    }
}
