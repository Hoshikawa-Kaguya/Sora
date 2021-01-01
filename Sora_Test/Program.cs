using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fleck;
using Sora.Entities.CQCodes;
using Sora.Enumeration.ApiType;
using Sora.Server;
using Sora.Server.ApiParams;
using Sora.Tool;

namespace Sora_Test
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            ConsoleLog.SetLogLevel(LogLevel.Debug);
            ConsoleLog.Debug("dotnet version",Environment.Version);

            SoraWSServer server = new SoraWSServer(new ServerConfig{Port = 8080,ApiTimeOut = 5000});
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
                                               if (eventArgs.SourceGroup == 883740678)
                                               {
                                                   var temp = eventArgs.Message.GetAllImage();
                                                   if (temp.Count != 0)
                                                   {
                                                       var res = await eventArgs.SoraApi.OcrImage(temp.First().ImgFile);
                                                       if (res.retCode == APIStatusType.OK)
                                                       {
                                                           await eventArgs.SourceGroup
                                                                          .SendGroupMessage(eventArgs.Message.MessageId,
                                                                              "\r\nocr res",
                                                                              $"\r\ncount = {res.texts.Count}",
                                                                              $"\r\nlang = {res.lang}");
                                                           List<CQCode> textList = new List<CQCode>();
                                                           foreach (TextDetection text in res.texts)
                                                           {
                                                               textList.Add(CQCode.CQText("c = "));
                                                               textList.Add(CQCode.CQText(text.Confidence.ToString()));
                                                               textList.Add(CQCode.CQText("\r\nt = "));
                                                               textList.Add(CQCode.CQText(text.Text));
                                                               textList.Add(CQCode.CQText("\r\n"));
                                                           }
                                                           await eventArgs.SourceGroup.SendGroupMessage(textList);
                                                       }
                                                   }
                                               }
                                           };
            //私聊消息事件
            server.Event.OnOfflineFileEvent += async (sender, eventArgs) =>
                                               {
                                                   await eventArgs.Sender.SendPrivateMessage("好耶");
                                               };
            try
            {
                await server.StartServer();
            }
            catch (Exception e) //侦测所有未处理的错误
            {
                ConsoleLog.Fatal("unknown error",ConsoleLog.ErrorLogBuilder(e));
            }
        }
    }
}
