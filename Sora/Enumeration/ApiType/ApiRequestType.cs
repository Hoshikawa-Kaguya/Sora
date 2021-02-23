using System.ComponentModel;

namespace Sora.Enumeration.ApiType
{
    /// <summary>
    /// API集合
    /// </summary>
    internal enum ApiRequestType
    {
        #region OnebotAPI

        /// <summary>
        /// 发送消息
        /// </summary>
        [Description("send_msg")] SendMsg,

        /// <summary>
        /// 获取登录号信息
        /// </summary>
        [Description("get_login_info")] GetLoginInfo,

        /// <summary>
        /// 获取版本信息
        /// </summary>
        [Description("get_version_info")] GetVersion,

        /// <summary>
        /// 撤回消息
        /// </summary>
        [Description("delete_msg")] RecallMsg,

        /// <summary>
        /// 获取好友列表
        /// </summary>
        [Description("get_friend_list")] GetFriendList,

        /// <summary>
        /// 获取群列表
        /// </summary>
        [Description("get_group_list")] GetGroupList,

        /// <summary>
        /// 获取群成员信息
        /// </summary>
        [Description("get_group_info")] GetGroupInfo,

        /// <summary>
        /// 获取群成员信息
        /// </summary>
        [Description("get_group_member_info")] GetGroupMemberInfo,

        /// <summary>
        /// 获取陌生人信息
        /// </summary>
        [Description("get_stranger_info")] GetStrangerInfo,

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        [Description("get_group_member_list")] GetGroupMemberList,

        /// <summary>
        /// 处理加好友请求
        /// </summary>
        [Description("set_friend_add_request")]
        SetFriendAddRequest,

        /// <summary>
        /// 处理加群请求/邀请
        /// </summary>
        [Description("set_group_add_request")] SetGroupAddRequest,

        /// <summary>
        /// 设置群名片
        /// </summary>
        [Description("set_group_card")] SetGroupCard,

        /// <summary>
        /// 设置群组专属头衔
        /// </summary>
        [Description("set_group_special_title")]
        SetGroupSpecialTitle,

        /// <summary>
        /// 群组T人
        /// </summary>
        [Description("set_group_kick")] SetGroupKick,

        /// <summary>
        /// 群组单人禁言
        /// </summary>
        [Description("set_group_ban")] SetGroupBan,

        /// <summary>
        /// 群全体禁言
        /// </summary>
        [Description("set_group_whole_ban")] SetGroupWholeBan,

        /// <summary>
        /// 群组匿名用户禁言
        /// </summary>
        [Description("set_group_anonymous_ban")]
        SetGroupAnonymousBan,

        /// <summary>
        /// 设置群管理员
        /// </summary>
        [Description("set_group_admin")] SetGroupAdmin,

        /// <summary>
        /// 群退出
        /// </summary>
        [Description("set_group_leave")] SetGroupLeave,

        /// <summary>
        /// 是否可以发送图片
        /// </summary>
        [Description("can_send_image")] CanSendImage,

        /// <summary>
        /// 是否可以发送语音
        /// </summary>
        [Description("can_send_record")] CanSendRecord,

        /// <summary>
        /// 获取插件运行状态
        /// </summary>
        [Description("get_status")] GetStatus,

        /// <summary>
        /// 重启客户端
        /// </summary>
        [Description("set_restart")] Restart,

        #endregion

        #region GoAPI

        /// <summary>
        /// 获取图片信息
        /// </summary>
        [Description("get_image")] GetImage,

        /// <summary>
        /// 获取消息
        /// </summary>
        [Description("get_msg")] GetMessage,

        /// <summary>
        /// 设置群名
        /// </summary>
        [Description("set_group_name")] SetGroupName,

        /// <summary>
        /// 获取合并转发消息
        /// </summary>
        [Description("get_forward_msg")] GetForwardMessage,

        /// <summary>
        /// 发送合并转发(群)
        /// </summary>
        [Description("send_group_forward_msg")]
        SendGroupForwardMsg,

        /// <summary>
        /// 设置群头像
        /// </summary>
        [Description("set_group_portrait")] SetGroupPortrait,

        /// <summary>
        /// 获取群系统消息
        /// </summary>
        [Description("get_group_system_msg")] GetGroupSystemMsg,

        /// <summary>
        /// 获取中文分词
        /// </summary>
        [Description(".get_word_slices")] GetWordSlices,

        /// <summary>
        /// 获取群文件系统信息
        /// </summary>
        [Description("get_group_file_system_info")]
        GetGroupFileSystemInfo,

        /// <summary>
        /// 获取群根目录文件列表
        /// </summary>
        [Description("get_group_root_files")] GetGroupRootFiles,

        /// <summary>
        /// 获取群子目录文件列表
        /// </summary>
        [Description("get_group_files_by_folder")]
        GetGroupFilesByFolder,

        /// <summary>
        /// 获取群文件资源链接
        /// </summary>
        [Description("get_group_file_url")] GetGroupFileUrl,

        /// <summary>
        /// 获取群@全体成员剩余次数
        /// </summary>
        [Description("get_group_at_all_remain")]
        GetGroupAtAllRemain,

        /// <summary>
        /// 调用腾讯的OCR接口
        /// </summary>
        [Description("ocr_image")] Ocr,

        /// <summary>
        /// 下载文件到缓存目录
        /// </summary>
        [Description("download_file")] DownloadFile,

        /// <summary>
        /// 获取群消息历史记录
        /// </summary>
        [Description("get_group_msg_history")] GetGroupMsgHistory,

        /// <summary>
        /// 获取当前账号在线客户端列表
        /// </summary>
        [Description("get_online_clients")] GetOnlineClients,

        /// <summary>
        /// 重载事件过滤器
        /// </summary>
        [Description("reload_event_filter")] ReloadEventFilter,

        /// <summary>
        /// 上传群文件
        /// </summary>
        [Description("upload_group_file")] UploadGroupFile,

        /// <summary>
        /// 设置精华消息
        /// </summary>
        [Description("set_essence_msg")] SetEssenceMsg,

        /// <summary>
        /// 移出精华消息
        /// </summary>
        [Description("delete_essence_msg")] DeleteEssenceMsg,

        /// <summary>
        /// 获取精华消息列表
        /// </summary>
        [Description("get_essence_msg_list")] GetEssenceMsgList,

        /// <summary>
        /// 检查链接安全性
        /// </summary>
        [Description("check_url_safely")] CheckUrlSafely,

        /// <summary>
        /// 获取VIP信息
        /// </summary>
        [Description("_get_vip_info")] _GetVipInfo,

        /// <summary>
        /// 发送群公告
        /// </summary>
        [Description("_send_group_notice")] _SendGroupNotice

        #endregion
    }
}