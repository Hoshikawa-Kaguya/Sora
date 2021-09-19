using System;
using Newtonsoft.Json.Linq;
using Sora.Entities.MessageSegment.Segment;
using Sora.Enumeration;
using Sora.OnebotModel.ApiParams;

namespace Sora.Entities.MessageSegment
{
    /// <summary>
    /// 消息段结构体
    /// </summary>
    public readonly struct SegmentData
    {
        #region 属性

        /// <summary>
        /// 消息段类型
        /// </summary>
        public SegmentType MessageType { get; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public Type DataType { get; }

        /// <summary>
        /// 数据实例
        /// </summary>
        public BaseSegment Data { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造消息段实例
        /// </summary>
        /// <param name="segmentType">消息段类型</param>
        /// <param name="dataObject">数据</param>
        internal SegmentData(SegmentType segmentType, BaseSegment dataObject)
        {
            MessageType = segmentType;
            Data        = dataObject;
            DataType    = dataObject?.GetType();
        }

        #endregion

        #region 辅助函数

        /// <summary>
        /// 获取数据类型
        /// 用于将BaseSegment转换为可读结构体
        /// </summary>
        /// <returns>
        /// 数据结构体类型
        /// </returns>
        public new Type GetType() => DataType;

        #endregion

        #region 获取数据内容(仅用于序列化)

        internal OnebotMessageElement ToOnebotMessage() => new()
        {
            MsgType = MessageType,
            RawData = JObject.FromObject(Data)
        };

        #endregion

        #region 运算符重载

        /// <summary>
        /// 等于重载
        /// </summary>
        public static bool operator ==(SegmentData segmentL, SegmentData segmentR)
        {
            if (segmentL.Data is not null && segmentR.Data is not null)
                return segmentL.MessageType == segmentR.MessageType &&
                       JToken.DeepEquals(JToken.FromObject(segmentL.Data),
                                         JToken.FromObject(segmentR.Data));
            return segmentL.Data is null && segmentR.Data is null && segmentL.MessageType == segmentR.MessageType;
        }

        /// <summary>
        /// 不等于重载
        /// </summary>
        public static bool operator !=(SegmentData segmentL, SegmentData segmentR)
        {
            return !(segmentL == segmentR);
        }

        /// <summary>
        /// +运算重载
        /// </summary>
        public static MessageBody operator +(SegmentData segmentR, SegmentData segmentL)
        {
            var messages = new MessageBody { segmentR, segmentL };
            return messages;
        }

        /// <summary>
        /// +运算重载
        /// </summary>
        public static MessageBody operator +(MessageBody message, SegmentData segment)
        {
            message.Add(segment);
            return message;
        }

        /// <summary>
        /// 隐式类型转换
        /// </summary>
        public static implicit operator MessageBody(SegmentData segmentData)
        {
            return new() { segmentData };
        }

        #endregion

        #region 常用重载

        /// <summary>
        /// 比较重载
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is SegmentData segment)
            {
                return this == segment;
            }

            return false;
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(MessageType, Data);
        }

        #endregion
    }
}