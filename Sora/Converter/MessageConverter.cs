using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sora.Attributes;
using Sora.Entities;
using Sora.Entities.MessageSegment;
using Sora.Entities.MessageSegment.Segment;
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
        private static CQCode<BaseSegment> ParseMessageElement(OnebotMessageElement onebotMessageElement)
        {
            if (onebotMessageElement.RawData == null) return new CQCode<BaseSegment>(CQType.Unknown, null);
            try
            {
                var jsonObj = JObject.FromObject(onebotMessageElement.RawData);
                if (jsonObj.Count == 0) return new CQCode<BaseSegment>(CQType.Unknown, null);
                return onebotMessageElement.MsgType switch
                {
                    CQType.Text =>
                        new CQCode<BaseSegment>(CQType.Text, jsonObj.ToObject<TextSegment>()),
                    CQType.Face =>
                        new CQCode<BaseSegment>(CQType.Face, jsonObj.ToObject<FaceSegment>()),
                    CQType.Image =>
                        new CQCode<BaseSegment>(CQType.Image, jsonObj.ToObject<ImageSegment>()),
                    CQType.Record =>
                        new CQCode<BaseSegment>(CQType.Record, jsonObj.ToObject<RecordSegment>()),
                    CQType.At =>
                        new CQCode<BaseSegment>(CQType.At, jsonObj.ToObject<AtSegment>()),
                    CQType.Share =>
                        new CQCode<BaseSegment>(CQType.Share, jsonObj.ToObject<ShareSegment>()),
                    CQType.Reply =>
                        new CQCode<BaseSegment>(CQType.Reply, jsonObj.ToObject<ReplySegment>()),
                    CQType.Forward =>
                        new CQCode<BaseSegment>(CQType.Forward, jsonObj.ToObject<ForwardSegment>()),
                    CQType.Xml =>
                        new CQCode<BaseSegment>(CQType.Xml, jsonObj.ToObject<CodeSegment>()),
                    CQType.Json =>
                        new CQCode<BaseSegment>(CQType.Json, jsonObj.ToObject<CodeSegment>()),

                    _ => new CQCode<BaseSegment>(CQType.Unknown, new UnknownSegment
                    {
                        Content = jsonObj
                    })
                };
            }
            catch (Exception e)
            {
                Log.Error("Sora", Log.ErrorLogBuilder(e));
                Log.Error("Sora", $"Json CQ码转换错误 未知CQ码格式，出错CQ码{onebotMessageElement.MsgType},请向框架开发者反应此问题");
                return new CQCode<BaseSegment>(CQType.Unknown, null);
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