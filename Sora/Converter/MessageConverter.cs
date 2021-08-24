using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
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
        #region 上报消息处理

        /// <summary>
        /// 处理接收到的消息段
        /// </summary>
        /// <param name="onebotMessageElement">消息段</param>
        /// <returns>消息段列表</returns>
        private static CQCode ParseMessageElement(OnebotMessageElement onebotMessageElement)
        {
            if (onebotMessageElement.RawData == null) return new CQCode(CQType.Unknown, null);
            try
            {
                var jsonObj = JObject.FromObject(onebotMessageElement.RawData);
                if(jsonObj.Count == 0) return new CQCode(CQType.Unknown, null);
                return onebotMessageElement.MsgType switch
                {
                    CQType.Text =>
                        new CQCode(CQType.Text, jsonObj.ToObject<Text>()),
                    CQType.Face =>
                        new CQCode(CQType.Face, jsonObj.ToObject<Face>()),
                    CQType.Image =>
                        new CQCode(CQType.Image, jsonObj.ToObject<Image>()),
                    CQType.Record =>
                        new CQCode(CQType.Record, jsonObj.ToObject<Record>()),
                    CQType.At =>
                        new CQCode(CQType.At, jsonObj.ToObject<At>()),
                    CQType.Share =>
                        new CQCode(CQType.Share, jsonObj.ToObject<Share>()),
                    CQType.Reply =>
                        new CQCode(CQType.Reply, jsonObj.ToObject<Reply>()),
                    CQType.Forward =>
                        new CQCode(CQType.Forward, jsonObj.ToObject<Forward>()),
                    CQType.Xml =>
                        new CQCode(CQType.Xml, jsonObj.ToObject<Code>()),
                    CQType.Json =>
                        new CQCode(CQType.Json, jsonObj.ToObject<Code>()),

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