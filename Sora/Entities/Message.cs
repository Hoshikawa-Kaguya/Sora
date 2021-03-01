using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sora.Entities.Base;
using Sora.Entities.CQCodes;
using Sora.Entities.CQCodes.CQCodeModel;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;

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
        public List<CQCode> MessageList { get; }

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
        public int? MessageSequence { get; }

        #endregion

        #region 构造函数

        internal Message(Guid connectionGuid, int msgId, string text, List<CQCode> cqCodeList, long time, int font,
                         int? messageSequence) : base(connectionGuid)
        {
            this.MessageId       = msgId;
            this.RawText         = text;
            this.MessageList     = cqCodeList;
            this.Time            = time;
            this.Font            = font;
            this.MessageSequence = messageSequence;
        }

        #endregion

        #region 消息管理方法

        /// <summary>
        /// 撤回本条消息
        /// </summary>
        public async ValueTask<APIStatusType> RecallMessage()
        {
            return await base.SoraApi.RecallMessage(this.MessageId);
        }

        #endregion

        #region CQ码快捷方法

        /// <summary>
        /// 获取所有At的UID
        /// </summary>
        /// <returns>
        /// <para>At的uid列表</para>
        /// <para><see cref="List{T}"/>(T=<see cref="long"/>)</para>
        /// </returns>
        public List<long> GetAllAtList()
        {
            return MessageList.Where(cq => cq.Function == CQFunction.At)
                              .Select(cq => Convert.ToInt64(((At) cq.CQData).Traget ?? "-1"))
                              .ToList();
        }

        /// <summary>
        /// 获取语音URL
        /// 仅在消息为语音时有效
        /// </summary>
        /// <returns>语音文件url</returns>
        public string GetRecordUrl()
        {
            if (this.MessageList.Count != 1 || MessageList.First().Function != CQFunction.Record) return null;
            return ((Record) MessageList.First().CQData).Url;
        }

        /// <summary>
        /// 获取所有图片信息
        /// </summary>
        /// <returns>
        /// <para>图片信息结构体列表</para>
        /// <para><see cref="List{T}"/>(T=<see cref="Image"/>)</para>
        /// </returns>
        public List<Image> GetAllImage()
        {
            return MessageList.Where(cq => cq.Function == CQFunction.Image)
                              .Select(cq => (Image) cq.CQData)
                              .ToList();
        }

        /// <summary>
        /// 是否是转发消息
        /// </summary>
        public bool IsForwardMessage()
        {
            return MessageList.Count == 1 && MessageList.First().Function == CQFunction.Forward;
        }

        /// <summary>
        /// 获取合并转发的ID
        /// </summary>
        public string GetForwardMsgId()
        {
            return IsForwardMessage() ? ((Forward) MessageList.First().CQData).MessageId : null;
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
            return HashCode.Combine(MessageId, RawText, MessageList, Time, Font, MessageSequence);
        }

        #endregion
    }
}