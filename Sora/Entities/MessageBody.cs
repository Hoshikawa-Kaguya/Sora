using Newtonsoft.Json;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YukariToolBox.LightLog;

namespace Sora.Entities;

/// <summary>
/// 消息段
/// </summary>
public class MessageBody : IList<SoraSegment>
{
    #region 私有字段

    private readonly List<SoraSegment> _message = new();

    private static readonly Regex[] regices = InitializeRegex();

    #endregion

    #region 属性

    /// <summary>
    /// 消息段数量
    /// </summary>
    public int Count => _message.Count;

    /// <summary>
    /// 只读
    /// </summary>
    public bool IsReadOnly => false;

    #endregion

    #region 构造方法

    /// <summary>
    /// 构造消息段列表
    /// </summary>
    public MessageBody(List<SoraSegment> messages)
    {
        RemoveIllegalSegment(ref messages);
        _message.Clear();
        _message.AddRange(messages);
    }

    /// <summary>
    /// 构造消息段列表
    /// </summary>
    public MessageBody()
    {
    }

    #endregion

    #region List方法

    /// <summary>
    /// 迭代器
    /// </summary>
    IEnumerator<SoraSegment> IEnumerable<SoraSegment>.GetEnumerator()
    {
        return _message.GetEnumerator();
    }

    /// <summary>
    /// 迭代器
    /// </summary>
    public IEnumerator GetEnumerator()
    {
        return _message.GetEnumerator();
    }

    /// <summary>
    /// 添加消息段
    /// </summary>
    /// <param name="item">消息段</param>
    public void Add(SoraSegment item)
    {
        if (SegmentCheck(item))
            _message.Add(item);
        else throw new ArgumentOutOfRangeException(nameof(item), "cannnot add Unknown/Ignored segement");
    }

    /// <summary>
    /// 添加纯文本消息
    /// </summary>
    /// <param name="text">纯文本消息段</param>
    public void Add(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentOutOfRangeException(nameof(text), "cannot add empty string");
        _message.Add(SoraSegment.Text(text));
    }

    /// <summary>
    /// 清空消息段
    /// </summary>
    public void Clear()
    {
        _message.Clear();
    }

    /// <summary>
    /// 判断包含
    /// </summary>
    /// <param name="item">消息段</param>
    public bool Contains(SoraSegment item)
    {
        return _message.Contains(item);
    }

    /// <summary>
    /// 复制
    /// </summary>
    /// <param name="array">消息段数组</param>
    /// <param name="arrayIndex">索引</param>
    public void CopyTo(SoraSegment[] array, int arrayIndex)
    {
        _message.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// 删除消息段
    /// </summary>
    /// <param name="item">消息段</param>
    public bool Remove(SoraSegment item)
    {
        return _message.Remove(item);
    }

    /// <summary>
    /// 索引查找
    /// </summary>
    /// <param name="item">消息段</param>
    public int IndexOf(SoraSegment item)
    {
        return _message.IndexOf(item);
    }

    /// <summary>
    /// 插入消息段
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="item">消息段</param>
    public void Insert(int index, SoraSegment item)
    {
        if (SegmentCheck(item))
            _message.Insert(index, item);
        else throw new ArgumentOutOfRangeException(nameof(item), "cannnot insert Unknown/Ignored segement");
    }

    /// <summary>
    /// 删除消息段
    /// </summary>
    /// <param name="index">索引</param>
    public void RemoveAt(int index)
    {
        _message.RemoveAt(index);
    }

    /// <summary>
    /// AddRange
    /// </summary>
    public void AddRange(List<SoraSegment> segments)
    {
        RemoveIllegalSegment(ref segments);
        _message.AddRange(segments);
    }

    /// <summary>
    /// AddRange
    /// </summary>
    public void AddRange(MessageBody segments)
    {
        RemoveIllegalSegment(ref segments);
        _message.AddRange(segments);
    }

    /// <summary>
    /// RemoveAll
    /// </summary>
    public int RemoveAll(Predicate<SoraSegment> match)
    {
        return _message.RemoveAll(match);
    }

    /// <summary>
    /// 转普通列表
    /// </summary>
    public List<SoraSegment> ToList()
    {
        return _message;
    }

    #endregion

    #region 扩展方法

    /// <summary>
    /// <para>添加纯文本消息段</para>
    /// <para>消息段扩展</para>
    /// </summary>
    /// <param name="text">纯文本信息</param>
    public void AddText(string text)
    {
        _message.Add(SoraSegment.Text(text));
    }

    /// <summary>
    /// 将含有CQ码的字符串反序列化成MessageBody对象
    /// </summary>
    /// <param name="str">包含CQ码的字符串</param>
    /// <returns>生成的MessageBody对象</returns>
    public static MessageBody GetMessageBody(string str)
    {
        return new MessageBody(ToSoraSegments(str));
    }

    #endregion

    #region 运算重载

    /// <summary>
    /// 索引器
    /// </summary>
    /// <param name="index">索引</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出范围</exception>
    /// <exception cref="NullReferenceException">读取到了空消息段</exception>
    public SoraSegment this[int index]
    {
        get
        {
            if (_message.Count == 0 || index > _message.Count - 1 || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _message[index];
        }
        set
        {
            if (value.MessageType == SegmentType.Unknown || value.DataType == null)
                throw new NullReferenceException("message element is null");
            if (value.MessageType == SegmentType.Ignore)
                throw new NullReferenceException(nameof(value));
            _message[index] = value;
        }
    }

    /// <summary>
    /// 运算重载
    /// </summary>
    public static MessageBody operator +(MessageBody message, string text)
    {
        message.Add(SoraSegment.Text(text));
        return message;
    }

    /// <summary>
    /// 运算重载
    /// </summary>
    public static MessageBody operator +(string text, MessageBody message)
    {
        message.Insert(0, SoraSegment.Text(text));
        return message;
    }

    /// <summary>
    /// 运算重载
    /// </summary>
    public static MessageBody operator +(MessageBody messageL, MessageBody messageR)
    {
        messageL.AddRange(messageR);
        return messageL;
    }

    #endregion

    #region 隐式转换

    /// <summary>
    /// 隐式类型转换
    /// </summary>
    public static implicit operator MessageBody(string text)
    {
        return new MessageBody() {SoraSegment.Text(text)};
    }

    #endregion

    #region 类内部工具

    /// <summary>
    /// 将字符串反序列化为SoraSegment集合
    /// </summary>
    /// <param name="str">需要进行反序列化的字符串</param>
    /// <returns>SoraSegment集合</returns>
    private static List<SoraSegment> ToSoraSegments(string str)
    {
        // 分离为文本数组和CQ码数组
        string[] text     = regices[0].Replace(str, "\0").Split('\0');
        Match[]  code     = regices[0].Matches(str).ToArray();
        var      segments = new List<SoraSegment>();
        for (var i = 0; i < code.Length; i++)
        {
            segments.Add(SoraSegment.Text(text[i]));
            segments.Add(ToSoraSegment(code[i].Value));
        }

        segments.Add(SoraSegment.Text(text[code.Length]));
        return segments;
    }

    /// <summary>
    /// 将CQ码反序列化为SoraSegment对象
    /// </summary>
    /// <param name="str">CQ码字符串</param>
    /// <returns>生成的SoraSegment对象</returns>
    private static SoraSegment ToSoraSegment(string str)
    {
        var match = regices[0].Match(str);
        if (!match.Success) throw new Exception("无法解析所传入的字符串, 字符串非CQ码格式!");
        if (!Enum.TryParse<SegmentType>(match.Groups[1].Value, true, out var segmentType))
            segmentType = SegmentType.Unknown;
        var collection = regices[1].Matches(match.Groups[2].Value);
        var sb         = new StringBuilder();
        sb.Append('{');
        foreach (Match code in collection)
        {
            sb.Append("\"");
            sb.Append(code.Groups[1].Value);
            sb.Append("\":\"");
            sb.Append(code.Groups[2].Value);
            sb.Append("\",");
        }

        sb.Append('}');
        return segmentType switch
        {
            SegmentType.Text => new SoraSegment(SegmentType.Text,
                                                JsonConvert.DeserializeObject<TextSegment>(sb.ToString())),
            SegmentType.Face => new SoraSegment(SegmentType.Face,
                                                JsonConvert.DeserializeObject<FaceSegment>(sb.ToString())),
            SegmentType.Image => new SoraSegment(SegmentType.Image,
                                                 JsonConvert.DeserializeObject<ImageSegment>(sb.ToString())),
            SegmentType.Record => new SoraSegment(SegmentType.Record,
                                                  JsonConvert.DeserializeObject<RecordSegment>(sb.ToString())),
            SegmentType.At => new SoraSegment(SegmentType.At, JsonConvert.DeserializeObject<AtSegment>(sb.ToString())),
            SegmentType.Share => new SoraSegment(SegmentType.Share,
                                                 JsonConvert.DeserializeObject<ShareSegment>(sb.ToString())),
            SegmentType.Reply => new SoraSegment(SegmentType.Reply,
                                                 JsonConvert.DeserializeObject<ReplySegment>(sb.ToString())),
            SegmentType.Forward => new SoraSegment(SegmentType.Forward,
                                                   JsonConvert.DeserializeObject<ForwardSegment>(sb.ToString())),
            SegmentType.Xml => new SoraSegment(SegmentType.Xml,
                                               JsonConvert.DeserializeObject<CodeSegment>(sb.ToString())),
            SegmentType.Json => new SoraSegment(SegmentType.Json,
                                                JsonConvert.DeserializeObject<CodeSegment>(sb.ToString())),
            _ => new SoraSegment(SegmentType.Unknown, null)
        };
    }

    private static void RemoveIllegalSegment(ref MessageBody segmentDatas)
    {
        var iCount = segmentDatas.RemoveAll(s =>
                                                s.MessageType is SegmentType.Ignore or SegmentType.Unknown ||
                                                s.Data is null);
        if (iCount != 0) Log.Warning("MessageBody", $"已移除{iCount}个无效消息段");
    }

    private static void RemoveIllegalSegment(ref List<SoraSegment> segmentDatas)
    {
        var iCount = segmentDatas.RemoveAll(s =>
                                                s.MessageType is SegmentType.Ignore or SegmentType.Unknown ||
                                                s.Data is null);
        if (iCount != 0) Log.Warning("MessageBody", $"已移除{iCount}个无效消息段");
    }

    private static bool SegmentCheck(SoraSegment s)
    {
        return !(s.MessageType is SegmentType.Ignore or SegmentType.Unknown ||
                 s.Data is null);
    }

    /// <summary>
    /// 延时初始化正则表达式
    /// </summary>
    /// <returns></returns>
    private static Regex[] InitializeRegex()
    {
        // 此处延时加载, 以提升运行速度
        return new Regex[]
        {
            new(@"\[CQ:([A-Za-z]*)(?:(,[^\[\]]+))?\]", RegexOptions.Compiled), // 匹配CQ码
            new(@",([A-Za-z]+)=([^,\[\]]+)", RegexOptions.Compiled)            // 匹配键值对
        };
    }

    #endregion
}