using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Sora.Entities.CQCodes.CQCodeModel
{
    /// <summary>
    /// <para>自定义转发节点</para>
    /// <para>仅用于发送</para>
    /// </summary>
    public class CustomNode
    {
        /// <summary>
        /// 转发消息Id
        /// </summary>
        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public string MessageId { get; internal set; }

        /// <summary>
        /// 发送者显示名字
        /// </summary>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; internal set; }

        /// <summary>
        /// 发送者QQ号
        /// </summary>
        [JsonProperty(PropertyName = "uin", NullValueHandling = NullValueHandling.Ignore)]
        public string UserId { get; internal set; }

        /// <summary>
        /// 具体消息
        /// </summary>
        [JsonProperty(PropertyName = "content", NullValueHandling = NullValueHandling.Ignore)]
        internal object Messages { get; set; }

        /// <summary>
        /// 转发时间
        /// </summary>
        [JsonProperty(PropertyName = "time", NullValueHandling = NullValueHandling.Ignore)]
        internal string Time { get; set; }

        /// <summary>
        /// 消息段
        /// </summary>
        [JsonIgnore]
        public List<CQCode> MessageList { get; internal set; }

        /// <summary>
        /// 构造自定义节点
        /// </summary>
        /// <param name="messageId">消息ID</param>
        public CustomNode(int messageId)
        {
            MessageId = messageId.ToString();
            Name      = null;
            UserId    = null;
            Messages  = null;
            Time      = null;
        }

        /// <summary>
        /// 构造自定义节点
        /// </summary>
        /// <param name="name">发送者名</param>
        /// <param name="userId">发送者ID</param>
        /// <param name="customMessage">消息段</param>
        /// <param name="time">消息段转发时间</param>
        public CustomNode(string name, long userId, List<CQCode> customMessage, DateTimeOffset? time = null)
        {
            MessageId = null;
            Name      = name;
            UserId    = userId.ToString();
            Messages  = customMessage.Select(msg => msg.ToOnebotMessage()).ToList();
            Time      = $"{time?.ToUnixTimeSeconds() ?? DateTimeOffset.Now.ToUnixTimeSeconds()}";
        }

        /// <summary>
        /// 构造自定义节点
        /// </summary>
        /// <param name="name">发送者名</param>
        /// <param name="userId">发送者ID</param>
        /// <param name="cqString">CQ码字符串格式</param>
        /// <param name="time">消息段转发时间</param>
        public CustomNode(string name, long userId, string cqString, DateTimeOffset? time = null)
        {
            MessageId = null;
            Name      = name;
            UserId    = userId.ToString();
            Messages  = cqString;
            Time      = $"{time?.ToUnixTimeSeconds() ?? DateTimeOffset.Now.ToUnixTimeSeconds()}";
        }
    }
}