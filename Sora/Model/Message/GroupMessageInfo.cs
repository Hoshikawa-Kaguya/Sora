using System;

namespace Sora.Model.Message
{
    /// <summary>
    /// 群组消息实例
    /// 通过调用get_group_msg API获得
    /// </summary>
    public class GroupMessageInfo
    {
        internal Guid ConnectionGuid { get; set; }

        /// <summary>
        /// 消息ID
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// 消息真实ID
        /// </summary>
        public int RealId { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 发送时间戳
        /// </summary>
        public long Time { get; set; }

        /// <summary>
        /// 发送者名字
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// 发送者UID
        /// </summary>
        public long SenderId { get; set; }
    }
}
