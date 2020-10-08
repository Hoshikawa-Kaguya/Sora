using System;
using System.Collections.Generic;
using Sora.Enumeration;
using Sora.Model.CQCode.CQCodeModel;
using Sora.Tool;

namespace Sora.Model.Message
{
    internal static class MessageParse
    {
        /// <summary>
        /// 处理接收到的消息段
        /// </summary>
        /// <param name="message">消息段</param>
        /// <returns>消息段列表</returns>
        internal static CQCode.CQCode ParseMessageElement(OnebotMessage message)
        {
            if (message == null || message.RawData.Count == 0) return null;
            try
            {
                switch (message.MsgType)
                {
                    case CQFunction.Text:
                        return new CQCode.CQCode(CQFunction.Text,message.RawData.ToObject<Text>());
                    case CQFunction.Face:
                        return new CQCode.CQCode(CQFunction.Face,message.RawData.ToObject<Face>());
                    case CQFunction.Image:
                        return new CQCode.CQCode(CQFunction.Image,message.RawData.ToObject<Image>());
                    case CQFunction.Record:
                        return new CQCode.CQCode(CQFunction.Record,message.RawData.ToObject<Record>());
                    case CQFunction.At:
                        return new CQCode.CQCode(CQFunction.At, message.RawData.ToObject<At>());
                    case CQFunction.Share:
                        return new CQCode.CQCode(CQFunction.Share,message.RawData.ToObject<Share>());
                    case CQFunction.Reply:
                        return new CQCode.CQCode(CQFunction.Reply,message.RawData.ToObject<Reply>());
                    case CQFunction.Forward:
                        return new CQCode.CQCode(CQFunction.Forward,message.RawData.ToObject<Forward>());
                    case CQFunction.Node:
                        return new CQCode.CQCode(CQFunction.Node,message.RawData.ToObject<Node>());
                    case CQFunction.Xml:
                        return new CQCode.CQCode(CQFunction.Xml,message.RawData.ToObject<Code>());
                    case CQFunction.Json:
                        return new CQCode.CQCode(CQFunction.Json,message.RawData.ToObject<Code>());
                    default:
                        return new CQCode.CQCode(CQFunction.Unknown, message.RawData);
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Sora",ConsoleLog.ErrorLogBuilder(e));
                ConsoleLog.Error("Sora",$"Json CQ码转换错误 未知CQ码格式，出错CQ码{message.MsgType},请向框架开发者反应此问题");
                return new CQCode.CQCode(CQFunction.Unknown, message.RawData);
            }
        }

        /// <summary>
        /// 处理消息段数组
        /// </summary>
        /// <param name="messages">消息段数组</param>
        internal static List<CQCode.CQCode> ParseMessageList(List<OnebotMessage> messages)
        {
            ConsoleLog.Debug("Sora","Parsing msg list");
            List<CQCode.CQCode> retMsg = new List<CQCode.CQCode>();
            foreach (OnebotMessage message in messages)
            {
                retMsg.Add(ParseMessageElement(message));
            }
            ConsoleLog.Debug("Sora",$"Get msg len={retMsg.Count}");
            return retMsg;
        }
    }
}
