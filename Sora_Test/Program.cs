using System;
using System.Collections.Generic;
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
            try
            {
                ConsoleLog.SetLogLevel(LogLevel.Debug);
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
                                                   if (eventArgs.Message.RawText.Equals("test"))
                                                   {
                                                       var test = await eventArgs.SoraApi.GetGroupRootFiles(eventArgs.SourceGroup);;
                                                       await eventArgs.SourceGroup
                                                                      .SendGroupMessage($"api = {test.apiStatus}\r\n");
                                                   }
                                                   else
                                                   {
                                                       List<CQCode> msg = new List<CQCode>();
                                                       msg.Add(CQCode.CQText("哇哦"));
                                                       msg.Add(CQCode.CQImage("https://i.loli.net/2020/10/20/zWPyocxFEVp2tDT.jpg"));
                                                       await eventArgs.SourceGroup.SendGroupMessage(msg);
                                                   }
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
