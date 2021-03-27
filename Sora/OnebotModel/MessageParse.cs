using System;
using System.Collections.Generic;
using System.Linq;
using Sora.Attributes;
using Sora.Entities.CQCodes;
using Sora.Entities.CQCodes.CQCodeModel;
using Sora.Enumeration;
using Sora.OnebotModel.ApiParams;
using YukariToolBox.FormatLog;

namespace Sora.OnebotModel
{
    internal static class MessageParse
    {
        /// <summary>
        /// 处理接收到的消息段
        /// </summary>
        /// <param name="messageElement">消息段</param>
        /// <returns>消息段列表</returns>
        [Reviewed("nidbCN", "2021-03-24 19:50")]
        private static CQCode ParseMessageElement(MessageElement messageElement)
        {
            if (messageElement?.RawData == null || messageElement.RawData.Count == 0) return null;

            try
            {
                return messageElement.MsgType switch
                {
                    CQFunction.Text =>
                        new CQCode(CQFunction.Text, messageElement.RawData.ToObject<Text>()),
                    CQFunction.Face =>
                        new CQCode(CQFunction.Face, messageElement.RawData.ToObject<Face>()),
                    CQFunction.Image =>
                        new CQCode(CQFunction.Image, messageElement.RawData.ToObject<Image>()),
                    CQFunction.Record =>
                        new CQCode(CQFunction.Record, messageElement.RawData.ToObject<Record>()),
                    CQFunction.At =>
                        new CQCode(CQFunction.At, messageElement.RawData.ToObject<At>()),
                    CQFunction.Share =>
                        new CQCode(CQFunction.Share, messageElement.RawData.ToObject<Share>()),
                    CQFunction.Reply =>
                        new CQCode(CQFunction.Reply, messageElement.RawData.ToObject<Reply>()),
                    CQFunction.Forward =>
                        new CQCode(CQFunction.Forward, messageElement.RawData.ToObject<Forward>()),
                    CQFunction.Xml =>
                        new CQCode(CQFunction.Xml, messageElement.RawData.ToObject<Code>()),
                    CQFunction.Json =>
                        new CQCode(CQFunction.Json, messageElement.RawData.ToObject<Code>()),

                    _ => new CQCode(CQFunction.Unknown, messageElement.RawData)
                };
            }
            catch (Exception e)
            {
                Log.Error("Sora", Log.ErrorLogBuilder(e));
                Log.Error("Sora", $"Json CQ码转换错误 未知CQ码格式，出错CQ码{messageElement.MsgType},请向框架开发者反应此问题");
                return new CQCode(CQFunction.Unknown, messageElement.RawData);
            }
        }

        /// <summary>
        /// 处理消息段数组
        /// </summary>
        /// <param name="messages">消息段数组</param>
        [Reviewed("nidbCN", "2021-03-24 19:49")]
        internal static List<CQCode> Parse(List<MessageElement> messages)
        {
            Log.Debug("Sora", "Parsing msg list");
            if (messages == null || messages.Count == 0) return new List<CQCode>();
            var retMsg = messages.Select(ParseMessageElement).ToList();

            Log.Debug("Sora", $"Get msg len={retMsg.Count}");
            return retMsg;
        }
    }
}