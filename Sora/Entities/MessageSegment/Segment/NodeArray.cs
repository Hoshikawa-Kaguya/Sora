using System.Collections.Generic;
using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Entities.MessageSegment.Segment
{
    /// <summary>
    /// 转发消息的列表
    /// </summary>
    public class NodeArray
    {
        /// <summary>
        /// 消息节点列表
        /// </summary>
        [JsonProperty(PropertyName = "messages")]
        public List<Node> NodeMsgList { get; internal set; } = new();

        #region Node处理

        /// <summary>
        /// 处理消息节点的消息为CQCode
        /// </summary>
        public void ParseNode()
        {
            NodeMsgList.ForEach(node => node.MessageBody = MessageConverter.Parse(node.MessageList));
        }

        #endregion
    }
}