using System;
using Newtonsoft.Json.Linq;
using YukariToolBox.Time;

namespace Sora.Entities.Info
{
    /// <summary>
    /// 精华消息信息
    /// </summary>
    public struct EssenceInfo
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public long MessageId { get; internal set; }

        /// <summary>
        /// 精华设置者
        /// </summary>
        public User Operator { get; internal set; }

        /// <summary>
        /// 精华设置者用户名
        /// </summary>
        public string OperatorName { get; internal set; }

        /// <summary>
        /// 精华设置时间
        /// </summary>
        public DateTime Time { get; internal set; }

        /// <summary>
        /// 消息发送者
        /// </summary>
        public User Sender { get; internal set; }

        /// <summary>
        /// 消息发送者名
        /// </summary>
        public string SenderName { get; internal set; }

        /// <summary>
        /// 消息发送时间
        /// </summary>
        public DateTime MessageSendTime { get; internal set; }

        internal EssenceInfo(JToken dataJson, Guid connection)
        {
            MessageId       = Convert.ToInt64(dataJson["message_id"] ?? 0);
            Operator        = new User(connection, Convert.ToInt64(dataJson["operator_id"] ?? 0));
            OperatorName    = dataJson["operator_nick"]?.ToString() ?? string.Empty;
            Time            = Convert.ToInt64(dataJson["operator_time"] ?? 0).ToDateTime();
            Sender          = new User(connection, Convert.ToInt64(dataJson["sender_id"] ?? 0));
            SenderName      = dataJson["sender_nick"]?.ToString() ?? string.Empty;
            MessageSendTime = Convert.ToInt64(dataJson["sender_time"] ?? 0).ToDateTime();
        }
    }
}
