using System.ComponentModel;

namespace Sora.Enumeration.ApiEnum
{
    internal enum APIType
    {
        /// <summary>
        /// 发送消息
        /// </summary>
        [Description("send_msg")]
        SendMsg,
        /// <summary>
        /// 获取登录号信息
        /// </summary>
        [Description("get_login_info")]
        GetLoginInfo,
        /// <summary>
        /// 获取版本信息
        /// </summary>
        [Description("get_version_info")]
        GetVersion,
        /// <summary>
        /// 获取合并转发消息
        /// </summary>
        [Description("get_forward_msg")]
        GetForwardMessage,
        /// <summary>
        /// 撤回消息
        /// </summary>
        [Description("delete_msg")]
        DeleteMsg,
        /// <summary>
        /// 获取好友列表
        /// </summary>
        [Description("get_friend_list")]
        GetFriendList,
        /// <summary>
        /// 获取群列表
        /// </summary>
        [Description("get_group_list")]
        GetGroupList,
    }
}
