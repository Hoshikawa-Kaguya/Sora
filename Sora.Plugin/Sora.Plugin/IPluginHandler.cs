using Sora.EventArgs.SoraEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sora.Plugin
{
    public interface IPluginHandler
    {
        ValueTask OnClientConnect(object sender, ConnectEventArgs eventArgs);
        ValueTask OnGroupMessage(object sender, GroupMessageEventArgs eventArgs);
        ValueTask OnPrivateMessage(object sender, PrivateMessageEventArgs eventArgs);
        ValueTask OnGroupRequest(object sender, AddGroupRequestEventArgs eventArgs);
        ValueTask OnFriendRequest(object sender, FriendRequestEventArgs eventArgs);
        ValueTask OnFileUpload(object sender, FileUploadEventArgs eventArgs);
        ValueTask OnGroupAdminChange(object sender, GroupAdminChangeEventArgs eventArgs);
        ValueTask OnGroupMemberChange(object sender, GroupMemberChangeEventArgs eventArgs);
        ValueTask OnGroupMemberMute(object sender, GroupMuteEventArgs eventArgs);
        ValueTask OnFriendAdd(object sender, FriendAddEventArgs eventArgs);
        ValueTask OnGroupRecall(object sender, GroupRecallEventArgs eventArgs);
        ValueTask OnFriendRecall(object sender, FriendRecallEventArgs eventArgs);
        ValueTask OnGroupCardUpdate(object sender, GroupCardUpdateEventArgs eventArgs);
        ValueTask OnGroupPoke(object sender, GroupPokeEventArgs eventArgs);
        ValueTask OnLuckyKingEvent(object sender, LuckyKingEventArgs eventArgs);
        ValueTask OnHonorEvent(object sender, HonorEventArgs eventArgs);
    }
}
