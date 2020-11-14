using System;
using System.Threading.Tasks;
using Fleck;
using Sora.Entities.CQCodes;
using Sora.Server;
using Sora.Tool;

namespace Sora_Test
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            ConsoleLog.SetLogLevel(LogLevel.Debug);
            ConsoleLog.Debug("dotnet version",Environment.Version);
            try
            {
                
                SoraWSServer server = new SoraWSServer(new ServerConfig{Port = 9200});
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
                                                   //新特性测试区域
                                                   await eventArgs.SourceGroup
                                                                  .SendGroupMessage(CQCode
                                                                      .CQImage("H:\\Desktop\\bot\\AntiRainDebug\\data\\image\\hso\\3956118_p0.jpg"));
                                               };
                server.Event.OnOfflineFileEvent += async (sender, eventArgs) =>
                                                   {
                                                       await eventArgs.Sender
                                                                      .SendPrivateMessage($"file url = {eventArgs.OfflineFileInfo.Url}");
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
