using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sora.Entities.Base;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;

namespace Sora.Entities;

/// <summary>
/// 消息类
/// </summary>
public sealed class MessageContext : BaseModel
{
#region 属性

    /// <summary>
    /// 消息ID
    /// </summary>
    public int MessageId { get; }

    /// <summary>
    /// <para>Gocq提供的纯文本信息</para>
    /// <para>可能会缺失某些不重要且会在相同消息中不相等的字段</para>
    /// </summary>
    public string RawText { get; internal set; }

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

    internal MessageContext(Guid        connectionId,
                            int         msgId,
                            string      text,
                            MessageBody messageBody,
                            long        time,
                            int         font,
                            long?       messageSequence)
        : base(connectionId)
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

#region 快捷方法

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
        List<long> uidList = MessageBody.Where(s => s.MessageType == SegmentType.At)
                                        .Select(s => long.TryParse((s.Data as AtSegment)?.Target ?? "0", out long uid)
                                                    ? uid
                                                    : -1).ToList();
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
        if (MessageBody.Count != 1 || MessageBody.First().MessageType != SegmentType.Record)
            return null;
        return (MessageBody.First().Data as RecordSegment)?.Url;
    }

    /// <summary>
    /// 获取所有图片信息
    /// </summary>
    /// <returns>
    /// <para>图片信息结构体列表</para>
    /// <para><see cref="List{T}"/>(T=<see cref="ImageSegment"/>)</para>
    /// </returns>
    public IEnumerable<ImageSegment> GetAllImage()
    {
        return MessageBody.Where(s => s.MessageType == SegmentType.Image).Select(s => s.Data as ImageSegment).ToList();
    }

    /// <summary>
    /// 是否为单图片消息
    /// </summary>
    public bool IsSingleImageMessage()
    {
        return MessageBody.Count == 1 && MessageBody[0].MessageType == SegmentType.Image;
    }

    /// <summary>
    /// 是否为纯图片消息
    /// </summary>
    public bool IsMultiImageMessage()
    {
        return MessageBody.Count > 1 && MessageBody.All(s => s.MessageType == SegmentType.Image);
    }

    /// <summary>
    /// 是否是转发消息
    /// </summary>
    public bool IsForwardMessage()
    {
        return MessageBody.Count == 1 && MessageBody[0].MessageType == SegmentType.Forward;
    }

    /// <summary>
    /// QQ小程序判断（Xml与Json类型消息）
    /// </summary>
    public bool IsCodeCard()
    {
        return MessageBody.Count == 1 && MessageBody[0].Data is CodeSegment;
    }

    /// <summary>
    /// 获取合并转发的ID
    /// </summary>
    public string GetForwardMsgId()
    {
        return IsForwardMessage() ? (MessageBody.First().Data as ForwardSegment)?.MessageId : null;
    }

    /// <summary>
    /// 截取消息中的文字部分
    /// </summary>
    public string GetText()
    {
        StringBuilder text = new();
        MessageBody.Where(s => s.MessageType == SegmentType.Text).Select(s => s.Data as TextSegment).ToList()
                   .ForEach(t => text.Append(t?.Content ?? string.Empty));
        return text.ToString();
    }

    /// <summary>
    /// 判定消息段相等
    /// </summary>
    public bool MessageEquals(MessageContext ctx)
    {
        if (ctx == null || ctx.MessageBody.Count != MessageBody.Count)
            return false;

        bool equal = true;
        for (int i = 0; i < MessageBody.Count; i++)
            equal &= MessageBody[i].Data == ctx.MessageBody[i].Data;

        return equal;
    }

    /// <summary>
    /// 判定消息段相等
    /// </summary>
    public bool MessageEquals(MessageBody msg)
    {
        if (msg == null || msg.Count != MessageBody.Count)
            return false;

        bool equal = true;
        for (int i = 0; i < MessageBody.Count; i++)
            equal &= MessageBody[i].Data == msg[i].Data;

        return equal;
    }

#endregion

#region 转换方法

    /// <summary>
    /// <para>转纯文本信息</para>
    /// <para>注意：消息段会被转换为onebot的string消息段格式</para>
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
    public static bool operator ==(MessageContext msgL, MessageContext msgR)
    {
        if (msgL is null && msgR is null)
            return true;

        return msgL is not null
               && msgR is not null
               && msgL.MessageId == msgR.MessageId
               && msgL.SoraApi == msgR.SoraApi
               && msgL.Font == msgR.Font
               && msgL.Time == msgR.Time
               && msgL.MessageSequence == msgR.MessageSequence
               && msgL.MessageBody.Count == msgR.MessageBody.Count
               && msgL.MessageEquals(msgR);
    }

    /// <summary>
    /// 不等于重载
    /// </summary>
    public static bool operator !=(MessageContext msgL, MessageContext msgR)
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
        if (obj is MessageContext msg)
            return this == msg;

        return false;
    }

    /// <summary>
    /// GetHashCode
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(MessageId, MessageBody, Time, Font, MessageSequence);
    }

#endregion

#region 索引器

    /// <summary>
    /// 索引器
    /// </summary>
    /// <param name="index">索引</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出范围</exception>
    /// <exception cref="NullReferenceException">读取到了空消息段</exception>
    public SoraSegment this[int index]
    {
        get => MessageBody[index];
        internal set => MessageBody[index] = value;
    }

#endregion
}