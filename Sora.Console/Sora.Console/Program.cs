using Sora.EventArgs.SoraEvent;
using Sora.Plugin;
using Sora.Tool;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using static Sora.ServerInterface.EventInterface;
using CBGanServer = Sora.SoraWSServer;

var server =  CBGanServer.CreateDefault();
var pluginsDirectoryInfo = new DirectoryInfo("Plugins");
foreach (var pluginDirectoryInfo in pluginsDirectoryInfo.GetDirectories())
{
    var config_file = pluginDirectoryInfo.GetFiles("info.json").FirstOrDefault();
    if (config_file == null) continue;
    try
    {
        var config_json = File.ReadAllText(config_file.FullName);
        var pluginInfo = JsonSerializer.Deserialize<PluginInfo>(config_json);
        if (!pluginInfo.Enable) continue;
        var flag = false;
        var flagMessage = "";
        try
        {
            var assembly = Assembly.LoadFile(Path.Combine(pluginDirectoryInfo.FullName, pluginInfo.Assembly));
            var handlerTypes = assembly.GetExportedTypes()
                .Where(x => typeof(IPluginHandler).IsAssignableFrom(x))
                .Where(x => x.IsClass).Where(x => !x.IsAbstract).ToList();
            foreach (var handlerType in handlerTypes)
            {
                var instance = Activator.CreateInstance(handlerType) as IPluginHandler;
                server.Event.OnClientConnect += new EventAsyncCallBackHandler<ConnectEventArgs>(instance.OnClientConnect);
                server.Event.OnGroupMessage += new EventAsyncCallBackHandler<GroupMessageEventArgs>(instance.OnGroupMessage);
                server.Event.OnPrivateMessage += new EventAsyncCallBackHandler<PrivateMessageEventArgs>(instance.OnPrivateMessage);
                server.Event.OnGroupRequest += new EventAsyncCallBackHandler<AddGroupRequestEventArgs>(instance.OnGroupRequest);
                server.Event.OnFriendRequest += new EventAsyncCallBackHandler<FriendRequestEventArgs>(instance.OnFriendRequest);
                server.Event.OnFileUpload += new EventAsyncCallBackHandler<FileUploadEventArgs>(instance.OnFileUpload);
                server.Event.OnGroupAdminChange += new EventAsyncCallBackHandler<GroupAdminChangeEventArgs>(instance.OnGroupAdminChange);
                server.Event.OnGroupMemberChange += new EventAsyncCallBackHandler<GroupMemberChangeEventArgs>(instance.OnGroupMemberChange);
                server.Event.OnGroupMemberMute += new EventAsyncCallBackHandler<GroupMuteEventArgs>(instance.OnGroupMemberMute);
                server.Event.OnFriendAdd += new EventAsyncCallBackHandler<FriendAddEventArgs>(instance.OnFriendAdd);
                server.Event.OnGroupRecall += new EventAsyncCallBackHandler<GroupRecallEventArgs>(instance.OnGroupRecall);
                server.Event.OnFriendRecall += new EventAsyncCallBackHandler<FriendRecallEventArgs>(instance.OnFriendRecall);
                server.Event.OnGroupCardUpdate += new EventAsyncCallBackHandler<GroupCardUpdateEventArgs>(instance.OnGroupCardUpdate);
                server.Event.OnGroupPoke += new EventAsyncCallBackHandler<GroupPokeEventArgs>(instance.OnGroupPoke);
                server.Event.OnLuckyKingEvent += new EventAsyncCallBackHandler<LuckyKingEventArgs>(instance.OnLuckyKingEvent);
                server.Event.OnHonorEvent += new EventAsyncCallBackHandler<HonorEventArgs>(instance.OnHonorEvent);
            }
            flag = true;
        }
        catch (Exception ex)
        {
            flagMessage = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
        }
        finally
        {
            ConsoleLog.Info("Sora", $"Sora 加载插件 [{pluginInfo.Name}] {(flag ? "成功" : $"失败:{Environment.NewLine}{flagMessage}")}");
        }
    }catch(Exception ex)
    {
        ConsoleLog.Info("Sora", $"Sora 加载插件配置 {config_file} 出错");
    }
}
await server.StartAsync();