using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Enumeration.ApiEnum;
using Sora.Module.CQCodes;
using Sora.Module.CQCodes.CQCodeModel;
using Sora.OnebotInterface;

namespace Sora.Module.SoraModel.Base
{
    /// <summary>
    /// Sora API执行实例
    /// </summary>
    public sealed class SoraApi
    {
        #region 属性
        /// <summary>
        /// 当前实例对应的链接GUID
        /// 用于调用API
        /// </summary>
        private Guid ConnectionGuid { get; set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化Api实例
        /// </summary>
        /// <param name="connectionGuid"></param>
        internal SoraApi(Guid connectionGuid)
        {
            this.ConnectionGuid = connectionGuid;
        }
        #endregion

        #region 消息API
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="userId">发送目标用户id</param>
        /// <param name="message">消息</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> SendPrivateMessage(long userId, params object[] message)
        {
            if(userId < 10000) throw new ArgumentOutOfRangeException($"{nameof(userId)} too small");
            if(message.Length == 0) throw new NullReferenceException(nameof(message));
            //消息段列表
            List<CQCode> msgList = new List<CQCode>();
            foreach (object msgObj in message)
            {
                if(msgObj is CQCode cqCode)
                {
                    msgList.Add(cqCode);
                }
                else
                {
                    msgList.Add(CQCode.CQText(msgObj.ToString()));
                }
            }
            return ((APIStatusType apiStatus, int messageId)) await ApiInterface.SendPrivateMessage(this.ConnectionGuid, userId, msgList);
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="groupId">发送目标群id</param>
        /// <param name="message">消息</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> SendGroupMessage(long groupId, params object[] message)
        {
            if(message.Length == 0) throw new NullReferenceException(nameof(message));
            //消息段列表
            List<CQCode> msgList = new List<CQCode>();
            foreach (object msgObj in message)
            {
                if(msgObj is CQCode cqCode)
                {
                    msgList.Add(cqCode);
                }
                else
                {
                    msgList.Add(CQCode.CQText(msgObj.ToString()));
                }
            }
            return ((APIStatusType apiStatus, int messageId))
                await ApiInterface.SendGroupMessage(this.ConnectionGuid, groupId, msgList);
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        public async ValueTask DeleteMessage(int messageId)
        {
            await ApiInterface.DeleteMsg(this.ConnectionGuid, messageId);
        }

        /// <summary>
        /// 获取合并转发消息
        /// </summary>
        /// <param name="forwardId"></param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="nodeArray"/> 消息节点列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<Node> nodeArray)> GetForwardMessage(string forwardId)
        {
            var apiResult = await ApiInterface.GetForwardMessage(this.ConnectionGuid, forwardId);
            return ((APIStatusType) apiResult.retCode, apiResult.nodeArray.NodeMsgList);
        }
        #endregion

        #region 账号信息API
        /// <summary>
        /// <para>获取登陆QQ的名字</para>
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="nick"/> 账号昵称</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, string nick)> GetLoginUserName()
        {
            (int retCode,_,string nick) = await ApiInterface.GetLoginInfo(this.ConnectionGuid);
            return ((APIStatusType)retCode, nick);
        }

        /// <summary>
        /// 获取好友列表
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="friendList"/> 好友列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<FriendInfo> friendList)> GetFriendList()
        {
            return ((APIStatusType apiStatus, List<FriendInfo> friendList)) 
                await ApiInterface.GetFriendList(this.ConnectionGuid);
        }

        /// <summary>
        /// 获取群组列表
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="groupList"/> 群组列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<GroupInfo> groupList)> GetGroupList()
        {
            return ((APIStatusType apiStatus, List<GroupInfo> groupList)) 
                await ApiInterface.GetGroupList(this.ConnectionGuid);
        }

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="groupMemberList"/> 群成员列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<GroupMemberInfo> groupMemberList)> GetGroupMemberList(long groupId)
        {
            return ((APIStatusType apiStatus, List<GroupMemberInfo> groupMemberList)) 
                await ApiInterface.GetGroupMemberList(this.ConnectionGuid, groupId);
        }

        /// <summary>
        /// 获取群信息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="groupInfo"/> 群信息列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, GroupInfo groupInfo)> GetGroupInfo(long groupId, bool useCache = true)
        {
            return ((APIStatusType apiStatus, GroupInfo groupInfo)) 
                await ApiInterface.GetGroupInfo(this.ConnectionGuid, groupId, useCache);
        }

        /// <summary>
        /// 获取群成员信息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户ID</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="memberInfo"/> 群成员信息</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, GroupMemberInfo memberInfo)> GetGroupMemberInfo(
            long groupId, long userId, bool useCache = true)
        {
            return ((APIStatusType apiStatus, GroupMemberInfo memberInfo)) 
                await ApiInterface.GetGroupMemberInfo(this.ConnectionGuid, groupId, userId, useCache);
        }
        #endregion

        #region 服务端API
        /// <summary>
        /// 获取连接客户端版本信息
        /// </summary>
        /// <returns>
        /// <para><see cref="ClientType"/>客户端类型</para>
        /// <para><see langword="ver"/>客户端版本号</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, ClientType clientType, string ver)> GetClientInfo()
        {
            return ((APIStatusType apiStatus, ClientType clientType, string ver))
                await ApiInterface.GetClientInfo(this.ConnectionGuid);
        }

        /// <summary>
        /// 检查是否可以发送图片
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="canSend"/> 是否能发送</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, bool canSend)> CanSendImage()
        {
            return ((APIStatusType apiStatus, bool canSend)) 
                await ApiInterface.CanSendImage(this.ConnectionGuid);
        }

        /// <summary>
        /// 检查是否可以发送语音
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="canSend"/> 是否能发送</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, bool canSend)> CanSendRecord()
        {
            return ((APIStatusType apiStatus, bool canSend)) 
                await ApiInterface.CanSendRecord(this.ConnectionGuid);
        }

        /// <summary>
        /// 获取客户端状态
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="online"/> 客户端是否在线</para>
        /// <para><see langword="good"/> 客户端是否正常运行</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, bool online, bool good)> GetStatus()
        {
            (int retCode, bool online, bool good, _) = await ApiInterface.GetStatus(this.ConnectionGuid);
            return ((APIStatusType) retCode, online, good);
        }

        /// <summary>
        /// 重启客户端
        /// 对go无效
        /// </summary>
        /// <param name="delay">延迟(ms)</param>
        public async ValueTask RebootClient(int delay = 0)
        {
            await ApiInterface.Restart(this.ConnectionGuid, delay);
        }

        /// <summary>
        /// 关闭服务端链接
        /// </summary>
        public void CloseConnection()
        {
            SoraWSServer.ConnectionInfos[this.ConnectionGuid].Close();
        }
        #endregion

        #region 请求处理API
        /// <summary>
        /// 处理加好友请求
        /// </summary>
        /// <param name="flag">请求flag</param>
        /// <param name="approve">是否同意</param>
        /// <param name="remark">好友备注</param>
        public async ValueTask SetFriendAddRequest(string flag, bool approve,
                                                   string remark = null)
        {
            await ApiInterface.SetFriendAddRequest(this.ConnectionGuid, flag, approve, remark);
        }

        /// <summary>
        /// 处理加群请求/邀请
        /// </summary>
        /// <param name="flag">请求flag</param>
        /// <param name="requestType">请求类型</param>
        /// <param name="approve">是否同意</param>
        /// <param name="reason">好友备注</param>
        public async ValueTask SetGroupAddRequest(string flag,
                                                  GroupRequestType requestType,
                                                  bool approve,
                                                  string reason = null)
        {
            await ApiInterface.SetGroupAddRequest(this.ConnectionGuid, flag, requestType, approve, reason);
        }
        #endregion

        #region 辅助API
        /// <summary>
        /// 获取中文分词
        /// </summary>
        /// <param name="text">内容</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="wordList"/> 分词列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<string> wordList)> GetWordSlices(string text)
        {
            return ((APIStatusType apiStatus, List<string> wordList)) await ApiInterface.GetWordSlices(this.ConnectionGuid, text);
        }
        #endregion
    }
}
