using System;
using System.Collections.Generic;
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
        private static CQCode ParseMessageElement(MessageElement messageElement)
        {
            if (messageElement?.RawData == null || messageElement.RawData.Count == 0) return null;
            try
            {
                switch (messageElement.MsgType)
                {
                    case CQFunction.Text:
                        return new CQCode(CQFunction.Text, messageElement.RawData.ToObject<Text>());
                    case CQFunction.Face:
                        return new CQCode(CQFunction.Face, messageElement.RawData.ToObject<Face>());
                    case CQFunction.Image:
                        return new CQCode(CQFunction.Image, messageElement.RawData.ToObject<Image>());
                    case CQFunction.Record:
                        return new CQCode(CQFunction.Record, messageElement.RawData.ToObject<Record>());
                    case CQFunction.At:
                        return new CQCode(CQFunction.At, messageElement.RawData.ToObject<At>());
                    case CQFunction.Share:
                        return new CQCode(CQFunction.Share, messageElement.RawData.ToObject<Share>());
                    case CQFunction.Reply:
                        return new CQCode(CQFunction.Reply, messageElement.RawData.ToObject<Reply>());
                    case CQFunction.Forward:
                        return new CQCode(CQFunction.Forward, messageElement.RawData.ToObject<Forward>());
                    case CQFunction.Xml:
                        return new CQCode(CQFunction.Xml, messageElement.RawData.ToObject<Code>());
                    case CQFunction.Json:
                        return new CQCode(CQFunction.Json, messageElement.RawData.ToObject<Code>());
                    default:
                        return new CQCode(CQFunction.Unknown, messageElement.RawData);
                }
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
        internal static List<CQCode> Parse(List<MessageElement> messages)
        {
            Log.Debug("Sora", "Parsing msg list");
            if (messages == null || messages.Count == 0) return new List<CQCode>();
            List<CQCode> retMsg = new();
            foreach (var message in messages)
            {
                retMsg.Add(ParseMessageElement(message));
            }

            Log.Debug("Sora", $"Get msg len={retMsg.Count}");
            return retMsg;
        }
    }
}