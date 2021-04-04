using System;
using Newtonsoft.Json.Linq;
using Sora.Enumeration;
using Sora.OnebotModel.ApiParams;

namespace Sora.Entities.MessageElement
{
    /// <summary>
    /// 消息段结构体
    /// </summary>
    public readonly struct CQCode
    {
        #region 属性

        /// <summary>
        /// CQ码类型
        /// </summary>
        public CQType MessageType { get; }

        /// <summary>
        /// CQ码数据实例
        /// </summary>
        public object DataObject { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造CQ码实例
        /// </summary>
        /// <param name="cqType">CQ码类型</param>
        /// <param name="dataObject"></param>
        internal CQCode(CQType cqType, object dataObject)
        {
            MessageType = cqType;
            DataObject  = dataObject;
        }

        #endregion

        #region 辅助函数

        /// <summary>
        /// 获取CQ码数据格式类型
        /// 用于将object转换为可读结构体
        /// </summary>
        /// <returns>
        /// 数据结构体类型
        /// </returns>
        public Type GetCqCodeDataType()
        {
            return DataObject.GetType();
        }

        #endregion

        #region 获取CQ码内容(仅用于序列化)

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
        public static bool operator ==(CQCode cqCodeL, CQCode cqCodeR)
        {
            return cqCodeL.MessageType == cqCodeR.MessageType &&
                   JToken.DeepEquals(JToken.FromObject(cqCodeL.DataObject), JToken.FromObject(cqCodeR.DataObject));
        }

        /// <summary>
        /// 不等于重载
        /// </summary>
        public static bool operator !=(CQCode cqCodeL, CQCode cqCodeR)
        {
            return !(cqCodeL == cqCodeR);
        }

        /// <summary>
        /// +运算重载
        /// </summary>
        public static MessageBody operator +(CQCode codeR, CQCode codeL)
        {
            var messages = new MessageBody {codeR, codeL};
            return messages;
        }

        /// <summary>
        /// +运算重载
        /// </summary>
        public static MessageBody operator +(MessageBody message, CQCode codeL)
        {
            message.Add(codeL);
            return message;
        }

        /// <summary>
        /// 隐式类型转换
        /// </summary>
        public static implicit operator MessageBody(CQCode cqCode)
        {
            return new() {cqCode};
        }

        /// <summary>
        /// 隐式类型转换
        /// </summary>
        public static implicit operator CQCode(string text)
        {
            return CQCodes.CQText(text);
        }

        #endregion

        #region 常用重载

        /// <summary>
        /// 比较重载
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is CQCode cqCode)
            {
                return this == cqCode;
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