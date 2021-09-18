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
        private static SoraSegment<BaseSegment> ParseMessageElement(OnebotMessageElement onebotMessageElement)
        {
            if (onebotMessageElement.RawData == null) return new SoraSegment<BaseSegment>(SegmentType.Unknown, null);
            try
            {
                var jsonObj = JObject.FromObject(onebotMessageElement.RawData);
                if (jsonObj.Count == 0) return new SoraSegment<BaseSegment>(SegmentType.Unknown, null);
                return onebotMessageElement.MsgType switch
                {
                    SegmentType.Text =>
                        new SoraSegment<BaseSegment>(SegmentType.Text, jsonObj.ToObject<TextSegment>()),
                    SegmentType.Face =>
                        new SoraSegment<BaseSegment>(SegmentType.Face, jsonObj.ToObject<FaceSegment>()),
                    SegmentType.Image =>
                        new SoraSegment<BaseSegment>(SegmentType.Image, jsonObj.ToObject<ImageSegment>()),
                    SegmentType.Record =>
                        new SoraSegment<BaseSegment>(SegmentType.Record, jsonObj.ToObject<RecordSegment>()),
                    SegmentType.At =>
                        new SoraSegment<BaseSegment>(SegmentType.At, jsonObj.ToObject<AtSegment>()),
                    SegmentType.Share =>
                        new SoraSegment<BaseSegment>(SegmentType.Share, jsonObj.ToObject<ShareSegment>()),
                    SegmentType.Reply =>
                        new SoraSegment<BaseSegment>(SegmentType.Reply, jsonObj.ToObject<ReplySegment>()),
                    SegmentType.Forward =>
                        new SoraSegment<BaseSegment>(SegmentType.Forward, jsonObj.ToObject<ForwardSegment>()),
                    SegmentType.Xml =>
                        new SoraSegment<BaseSegment>(SegmentType.Xml, jsonObj.ToObject<CodeSegment>()),
                    SegmentType.Json =>
                        new SoraSegment<BaseSegment>(SegmentType.Json, jsonObj.ToObject<CodeSegment>()),

                    _ => new SoraSegment<BaseSegment>(SegmentType.Unknown, new UnknownSegment
                    {
                        Content = jsonObj
                    })
                };
            }
            catch (Exception e)
            {
                Log.Error("Sora", Log.ErrorLogBuilder(e));
                Log.Error("Sora", $"JsonSegment转换错误 未知格式，出错类型[{onebotMessageElement.MsgType}],请向框架开发者反应此问题");
                return new SoraSegment<BaseSegment>(SegmentType.Unknown, null);
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