using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sora.Entities.Base;
using Sora.Entities.Info;
using Sora.Entities.MessageElement.CQModel;
using Sora.Enumeration;

namespace Sora.Entities
{
    /// <summary>
    /// 消息类
    /// </summary>
    public sealed class Message : BaseModel
    {
        #region 属性

        /// <summary>
        /// 消息ID
        /// </summary>
        public int MessageId { get; }

        /// <summary>
        /// 纯文本信息
        /// </summary>
        public string RawText { get; }

        /// <summary>
        /// 消息段列表
        /// </summary>
        public MessageBody MessageBody { get; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public long Time { get; }

        /// <summary>
        /// 消息字体id
        /// </summary>
        public int Font { get; }

        /// <summary>
        /// <para>消息序号</para>
        /// <para>仅用于群聊消息</para>
        /// </summary>
        public long? MessageSequence { get; }

        #endregion

        #region 构造函数

        internal Message(Guid serviceId, Guid connectionId, int msgId, string text, MessageBody messageBody,
                         long time, int font, long? messageSequence) :
            base(serviceId, connectionId)
        {
            MessageId       = msgId;
            RawText         = text;
            MessageBody     = messageBody;
            Time            = time;
            Font            = font;
            MessageSequence = messageSequence;
        }

        #endregion

        #region 消息管理方法

        /// <summary>
        /// 撤回本条消息
        /// </summary>
        public async ValueTask<ApiStatus> RecallMessage()
        {
            return await SoraApi.RecallMessage(MessageId);
        }

        /// <summary>
        /// 标记此消息已读
        /// </summary>
        public async ValueTask<ApiStatus> MarkMessageRead()
        {
            return await SoraApi.MarkMessageRead(MessageId);
        }

        #endregion

        #region CQ码快捷方法

        /// <summary>
        /// 获取所有At的UID
        /// Notice:at全体不会被转换
        /// </summary>
        /// <returns>
        /// <para>At的uid列表</para>
        /// <para><see cref="List{T}"/>(T=<see cref="long"/>)</para>
        /// </returns>
        public IEnumerable<long> GetAllAtList()
        {
            var uidList = MessageBody.Where(cq => cq.MessageType == CQType.At)
                                     .Select(cq => long.TryParse(((At)cq.DataObject).Traget, out var uid) ? uid : -1)
                                     .ToList();
            //去除无法转换的值，如at全体
            uidList.RemoveAll(uid => uid == -1);
            return uidList;
        }

        /// <summary>
        /// 获取语音URL
        /// 仅在消息为语音时有效
        /// </summary>
        /// <returns>语音文件url</returns>
        public string GetRecordUrl()
        {
            if (MessageBody.Count != 1 || MessageBody.First().MessageType != CQType.Record) return null;
            return ((Record)MessageBody.First().DataObject).Url;
        }

        /// <summary>
        /// 获取所有图片信息
        /// </summary>
        /// <returns>
        /// <para>图片信息结构体列表</para>
        /// <para><see cref="List{T}"/>(T=<see cref="Image"/>)</para>
        /// </returns>
        public IEnumerable<Image> GetAllImage()
        {
            return MessageBody.Where(cq => cq.MessageType == CQType.Image)
                              .Select(cq => (Image)cq.DataObject)
                              .ToList();
        }

        /// <summary>
        /// 是否是转发消息
        /// </summary>
        public bool IsForwardMessage()
        {
            return MessageBody.Count == 1 && MessageBody.First().MessageType == CQType.Forward;
        }

        /// <summary>
        /// 获取合并转发的ID
        /// </summary>
        public string GetForwardMsgId()
        {
            return IsForwardMessage() ? ((Forward)MessageBody.First().DataObject).MessageId : null;
        }

        #endregion

        #region 转换方法

        /// <summary>
        /// <para>转纯文本信息</para>
        /// <para>注意：CQ码会被转换为onebot的string消息段格式</para>
        /// </summary>
        public override string ToString()
        {
            return RawText;
        }

        #endregion

        #region 运算符重载

        /// <summary>
        /// 等于重载
        /// </summary>
        public static bool operator ==(Message msgL, Message msgR)
        {
            if (msgL is null && msgR is null) return true;

            return msgL is not null                             &&
                   msgR is not null                             &&
                   msgL.MessageId       == msgR.MessageId       &&
                   msgL.SoraApi         == msgR.SoraApi         &&
                   msgL.Font            == msgR.Font            &&
                   msgL.Time            == msgR.Time            &&
                   msgL.MessageSequence == msgR.MessageSequence &&
                   msgL.RawText.Equals(msgR.RawText);
        }

        /// <summary>
        /// 不等于重载
        /// </summary>
        public static bool operator !=(Message msgL, Message msgR)
        {
            return !(msgL == msgR);
        }

        #endregion

        #region 常用重载

        /// <summary>
        /// 比较重载
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Message msg)
            {
                return this == msg;
            }

            return false;
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(MessageId, RawText, MessageBody, Time, Font, MessageSequence);
        }

        #endregion
    }
}