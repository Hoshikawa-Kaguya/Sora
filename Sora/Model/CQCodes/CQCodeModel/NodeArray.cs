using System.Collections.Generic;
using Newtonsoft.Json;
using Sora.Model.Message;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// 转发消息的列表
    /// </summary>
    internal class NodeArray
    {
        /// <summary>
        /// 消息节点列表
        /// </summary>
        [JsonProperty(PropertyName = "messages")]
        internal List<Node> NodeMsgList { get; set; }

        #region Node处理
        /// <summary>
        /// 处理消息节点的消息为CQCode
        /// </summary>
        internal void ParseNode()
        {
            this.NodeMsgList.ForEach(node => node.CQCodeMsgList = MessageParse.ParseMessageList(node.MessageList));
        }
        #endregion
    }
}
