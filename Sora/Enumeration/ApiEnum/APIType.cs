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
    }
}
