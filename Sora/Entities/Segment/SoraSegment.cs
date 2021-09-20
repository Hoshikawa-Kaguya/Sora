using System;
using Newtonsoft.Json.Linq;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.OnebotModel.ApiParams;

namespace Sora.Entities.Segment
{
    /// <summary>
    /// 消息段结构体
    /// </summary>
    public readonly struct SoraSegment
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
        internal SoraSegment(SegmentType segmentType, BaseSegment dataObject)
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

        internal OnebotSegment ToOnebotMessage() => new()
        {
            MsgType = MessageType,
            RawData = JObject.FromObject(Data)
        };

        #endregion

        #region 运算符重载

        /// <summary>
        /// 等于重载
        /// </summary>
        public static bool operator ==(SoraSegment soraSegmentL, SoraSegment soraSegmentR)
        {
            if (soraSegmentL.Data is not null && soraSegmentR.Data is not null)
                return soraSegmentL.MessageType == soraSegmentR.MessageType &&
                       JToken.DeepEquals(JToken.FromObject(soraSegmentL.Data),
                                         JToken.FromObject(soraSegmentR.Data));
            return soraSegmentL.Data is null && soraSegmentR.Data is null &&
                   soraSegmentL.MessageType == soraSegmentR.MessageType;
        }

        /// <summary>
        /// 不等于重载
        /// </summary>
        public static bool operator !=(SoraSegment soraSegmentL, SoraSegment soraSegmentR)
        {
            return !(soraSegmentL == soraSegmentR);
        }

        /// <summary>
        /// +运算重载
        /// </summary>
        public static MessageBody operator +(SoraSegment soraSegmentR, SoraSegment soraSegmentL)
        {
            var messages = new MessageBody { soraSegmentR, soraSegmentL };
            return messages;
        }

        /// <summary>
        /// +运算重载
        /// </summary>
        public static MessageBody operator +(MessageBody message, SoraSegment soraSegment)
        {
            message.Add(soraSegment);
            return message;
        }

        /// <summary>
        /// 隐式类型转换
        /// </summary>
        public static implicit operator MessageBody(SoraSegment soraSegment)
        {
            return new() { soraSegment };
        }

        #endregion

        #region 常用重载

        /// <summary>
        /// 比较重载
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is SoraSegment segment)
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