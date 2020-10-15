using Sora.EventArgs.SoraEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sora.Plugin
{
    public abstract class BasePluginHandler : IPluginHandler
    {
        public virtual ValueTask OnClientConnect(object sender, ConnectEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnFileUpload(object sender, FileUploadEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnFriendAdd(object sender, FriendAddEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnFriendRecall(object sender, FriendRecallEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnFriendRequest(object sender, FriendRequestEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnGroupAdminChange(object sender, GroupAdminChangeEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnGroupCardUpdate(object sender, GroupCardUpdateEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnGroupMemberChange(object sender, GroupMemberChangeEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnGroupMemberMute(object sender, GroupMuteEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnGroupPoke(object sender, GroupPokeEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnGroupRecall(object sender, GroupRecallEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnGroupRequest(object sender, AddGroupRequestEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnHonorEvent(object sender, HonorEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnLuckyKingEvent(object sender, LuckyKingEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnPrivateMessage(object sender, PrivateMessageEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask OnGroupMessage(object sender, GroupMessageEventArgs eventArgs)
        {
            return ValueTask.CompletedTask;
        }
    }
}
