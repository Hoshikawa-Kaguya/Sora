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
    public class SoraSegment<T> where T : BaseSegment
    {
        #region 属性

        /// <summary>
        /// 消息段类型
        /// </summary>
        public SegmentType MessageType { get; }

        /// <summary>
        /// 数据实例
        /// </summary>
        public T DataObject { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造消息段实例
        /// </summary>
        /// <param name="segmentType">消息段类型</param>
        /// <param name="dataObject">数据</param>
        internal SoraSegment(SegmentType segmentType, T dataObject)
        {
            MessageType = segmentType;
            DataObject  = dataObject;
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
        public Type GetSegmentType()
        {
            return typeof(T);
        }

        #endregion

        #region 获取数据内容(仅用于序列化)

        internal OnebotMessageElement ToOnebotMessage() => new()
        {
            MsgType = MessageType,
            RawData = JObject.FromObject(DataObject)
        };

        #endregion

        #region 运算符重载

        /// <summary>
        /// 等于重载
        /// </summary>
        public static bool operator ==(SoraSegment<T> soraSegmentL, SoraSegment<T> soraSegmentR)
        {
            if (soraSegmentL is not null && soraSegmentR is not null)
                return soraSegmentL.MessageType == soraSegmentR.MessageType &&
                       JToken.DeepEquals(JToken.FromObject(soraSegmentL.DataObject),
                                         JToken.FromObject(soraSegmentR.DataObject));
            return soraSegmentL is null && soraSegmentR is null;
        }

        /// <summary>
        /// 不等于重载
        /// </summary>
        public static bool operator !=(SoraSegment<T> soraSegmentL, SoraSegment<T> soraSegmentR)
        {
            return !(soraSegmentL == soraSegmentR);
        }

        /// <summary>
        /// +运算重载
        /// </summary>
        public static MessageBody operator +(SoraSegment<T> codeR, SoraSegment<T> codeL)
        {
            var messages = new MessageBody { codeR, codeL };
            return messages;
        }

        /// <summary>
        /// +运算重载
        /// </summary>
        public static MessageBody operator +(MessageBody message, SoraSegment<T> codeL)
        {
            message.Add(codeL);
            return message;
        }

        /// <summary>
        /// 隐式类型转换
        /// </summary>
        public static implicit operator MessageBody(SoraSegment<T> soraSegment)
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
            if (obj is SoraSegment<T> segment)
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
            return HashCode.Combine(MessageType, DataObject);
        }

        #endregion
    }
}