using System.ComponentModel;

namespace Sora.Enumeration.ApiEnum
{
    internal enum ApiMessageType
    {
        /// <summary>
        /// 私聊消息
        /// </summary>
        [Description("private")]
        Private = 1,

        /// <summary>
        /// 群消息
        /// </summary>
        [Description("group")]
        Group = 2
    }
}
