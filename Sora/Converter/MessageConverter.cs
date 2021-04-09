using System;
using System.Collections.Generic;
using System.Linq;
using Sora.Attributes;
using Sora.Entities;
using Sora.Entities.MessageElement;
using Sora.Entities.MessageElement.CQModel;
using Sora.Enumeration;
using Sora.OnebotModel.ApiParams;
using YukariToolBox.FormatLog;

namespace Sora.Converter
{
    /// <summary>
    /// 消息预处理扩展函数
    /// </summary>
    internal static class MessageConverter
    {
        #region 传入消息处理

        /// <summary>
        /// 预处理消息队列
        /// </summary>
        /// <param name="content">消息数据</param>
        internal static MessageBody ToCQCodeList(this object[] content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            MessageBody msgList = new();
            foreach (var msgObj in content)
            {
                switch (msgObj)
                {
                    case CQCode cqCode:
                        msgList.Add(cqCode);
                        break;
                    case IEnumerable<CQCode> cqCodes:
                        msgList.AddRange(cqCodes);
                        break;
                    case null:
                        msgList.Add(CQCodes.CQText("null"));
                        break;
                    default:
                        msgList.Add(CQCodes.CQText(msgObj.ToString()));
                        break;
                }
            }

            return msgList;
        }

        #endregion

        #region 上报消息处理

        /// <summary>
        /// 处理接收到的消息段
        /// </summary>
        /// <param name="onebotMessageElement">消息段</param>
        /// <returns>消息段列表</returns>
        [Reviewed("nidbCN", "2021-03-24 19:50")]
        private static CQCode ParseMessageElement(OnebotMessageElement onebotMessageElement)
        {
            if (onebotMessageElement?.RawData == null || onebotMessageElement.RawData.Count == 0)
                return new CQCode(CQType.Unknown, null);

            try
            {
                return onebotMessageElement.MsgType switch
                {
                    CQType.Text =>
                        new CQCode(CQType.Text, onebotMessageElement.RawData.ToObject<Text>()),
                    CQType.Face =>
                        new CQCode(CQType.Face, onebotMessageElement.RawData.ToObject<Face>()),
                    CQType.Image =>
                        new CQCode(CQType.Image, onebotMessageElement.RawData.ToObject<Image>()),
                    CQType.Record =>
                        new CQCode(CQType.Record, onebotMessageElement.RawData.ToObject<Record>()),
                    CQType.At =>
                        new CQCode(CQType.At, onebotMessageElement.RawData.ToObject<At>()),
                    CQType.Share =>
                        new CQCode(CQType.Share, onebotMessageElement.RawData.ToObject<Share>()),
                    CQType.Reply =>
                        new CQCode(CQType.Reply, onebotMessageElement.RawData.ToObject<Reply>()),
                    CQType.Forward =>
                        new CQCode(CQType.Forward, onebotMessageElement.RawData.ToObject<Forward>()),
                    CQType.Xml =>
                        new CQCode(CQType.Xml, onebotMessageElement.RawData.ToObject<Code>()),
                    CQType.Json =>
                        new CQCode(CQType.Json, onebotMessageElement.RawData.ToObject<Code>()),

                    _ => new CQCode(CQType.Unknown, onebotMessageElement.RawData)
                };
            }
            catch (Exception e)
            {
                Log.Error("Sora", Log.ErrorLogBuilder(e));
                Log.Error("Sora", $"Json CQ码转换错误 未知CQ码格式，出错CQ码{onebotMessageElement.MsgType},请向框架开发者反应此问题");
                return new CQCode(CQType.Unknown, onebotMessageElement.RawData);
            }
        }

        /// <summary>
        /// 处理消息段数组
        /// </summary>
        /// <param name="messages">消息段数组</param>
        [Reviewed("nidbCN", "2021-03-24 19:49")]
        internal static MessageBody Parse(List<OnebotMessageElement> messages)
        {
            Log.Debug("Sora", "Parsing msg list");
            if (messages == null || messages.Count == 0) return new MessageBody();
            var retMsg = messages.Select(ParseMessageElement).ToList();

            Log.Debug("Sora", $"Get msg len={retMsg.Count}");
            return new MessageBody(retMsg);
        }

        #endregion
    }
}