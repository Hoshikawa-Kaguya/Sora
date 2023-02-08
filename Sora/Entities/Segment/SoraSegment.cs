using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.OnebotModel.ApiParams;
using YukariToolBox.LightLog;

namespace Sora.Entities.Segment;

/// <summary>
/// 消息段结构体
/// </summary>
[ProtoContract]
public class SoraSegment
{
#region 属性

    /// <summary>
    /// 消息段类型
    /// </summary>
    [JsonProperty(PropertyName = "msg_t")]
    [ProtoMember(1)]
    public SegmentType MessageType { get; }

    /// <summary>
    /// 数据实例
    /// </summary>
    [JsonProperty(PropertyName = "data")]
    [ProtoMember(2)]
    public BaseSegment Data { get; }

#endregion 属性

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
    }

    //Default constructor for protobuf
    private SoraSegment()
    {
    }

#endregion 构造函数

#region 获取数据内容(仅用于序列化)

    internal OnebotSegment ToOnebotSegment()
    {
        return new OnebotSegment
        {
            MsgType = MessageType,
            RawData = JObject.FromObject(Data)
        };
    }

#endregion 获取数据内容(仅用于序列化)

#region 运算符重载

    /// <summary>
    /// 等于重载
    /// </summary>
    public static bool operator ==(SoraSegment soraSegmentL, SoraSegment soraSegmentR)
    {
        if (soraSegmentL.Data is not null && soraSegmentR.Data is not null)
            return soraSegmentL.MessageType == soraSegmentR.MessageType
                   && JToken.DeepEquals(JToken.FromObject(soraSegmentL.Data), JToken.FromObject(soraSegmentR.Data));
        return soraSegmentL.Data is null
               && soraSegmentR.Data is null
               && soraSegmentL.MessageType == soraSegmentR.MessageType;
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
        MessageBody messages = new()
        {
            soraSegmentR,
            soraSegmentL
        };
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
    /// +运算重载
    /// </summary>
    public static MessageBody operator +(SoraSegment soraSegment, MessageBody message)
    {
        message.Insert(0, soraSegment);
        return message;
    }

    /// <summary>
    /// +运算重载
    /// </summary>
    public static MessageBody operator +(string message, SoraSegment soraSegment)
    {
        return new MessageBody
        {
            message,
            soraSegment
        };
    }

    /// <summary>
    /// +运算重载
    /// </summary>
    public static MessageBody operator +(SoraSegment soraSegment, string message)
    {
        return new MessageBody
        {
            soraSegment,
            message
        };
    }

    /// <summary>
    /// 隐式类型转换
    /// </summary>
    public static implicit operator MessageBody(SoraSegment soraSegment)
    {
        return new MessageBody { soraSegment };
    }

#endregion 运算符重载

#region 常用重载

    /// <summary>
    /// 比较重载
    /// </summary>
    public override bool Equals(object obj)
    {
        if (obj is SoraSegment segment)
            return this == segment;

        return false;
    }

    /// <summary>
    /// GetHashCode
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(MessageType, Data);
    }

#endregion 常用重载

#region 隐式转换

    /// <summary>
    /// 隐式类型转换
    /// </summary>
    public static implicit operator SoraSegment(string text)
    {
        return Text(text);
    }

#endregion 隐式转换

#region 消息段生成/处理

    /// <summary>
    /// 纯文本
    /// </summary>
    /// <param name="msg">文本消息</param>
    public static SoraSegment Text(string msg)
    {
        return msg is null
            ? IllegalSegment("Text", "消息不能为空")
            : new SoraSegment(SegmentType.Text, new TextSegment { Content = msg });
    }

    /// <summary>
    /// At 消息段
    /// </summary>
    /// <param name="uid">用户uid</param>
    public static SoraSegment At(long uid)
    {
        return uid < 10000
            ? IllegalSegment("At", $"id超出范围限制({uid})")
            : new SoraSegment(SegmentType.At, new AtSegment { Target = uid.ToString() });
    }

    /// <summary>
    /// At 消息段
    /// </summary>
    /// <param name="uid">用户uid</param>
    /// <param name="name">当在群中找不到此uid的名称时使用的名字</param>
    public static SoraSegment At(long uid, string name)
    {
        return uid < 10000
            ? IllegalSegment("At", $"非法参数[id超出范围限制({uid})]")
            : new SoraSegment(SegmentType.At,
                              new AtSegment
                              {
                                  Target = uid.ToString(),
                                  Name   = name
                              });
    }

    /// <summary>
    /// At全体 消息段
    /// </summary>
    public static SoraSegment AtAll()
    {
        return new SoraSegment(SegmentType.At, new AtSegment { Target = "all" });
    }

    /// <summary>
    /// 表情 消息段
    /// </summary>
    /// <param name="id">表情 ID</param>
    public static SoraSegment Face(int id)
    {
        //检查ID合法性
        return id is < 0 or > 244
            ? IllegalSegment("Face", $"非法参数[id超出范围限制({id})]")
            : new SoraSegment(SegmentType.Face, new FaceSegment { Id = id });
    }

    /// <summary>
    /// 语音 消息段
    /// </summary>
    /// <param name="data">文件名/绝对路径/URL/base64</param>
    /// <param name="isMagic">是否为变声</param>
    /// <param name="useCache">是否使用已缓存的文件</param>
    /// <param name="useProxy">是否通过代理下载文件</param>
    /// <param name="timeout">超时时间，默认为 <see langword="null"/>(不超时)</param>
    /// <remarks>若要使用Base64,需要让 data 以 base64:// 开头</remarks>
    public static SoraSegment Record(string data,
                                     bool   isMagic  = false,
                                     bool   useCache = true,
                                     bool   useProxy = true,
                                     int?   timeout  = null)
    {
        (string dataStr, bool isDataStr) = SegmentHelper.ParseDataStr(data);
        return !isDataStr
            ? IllegalSegment("Record", $"非法数据字符串({data})")
            : new SoraSegment(SegmentType.Record,
                              new RecordSegment
                              {
                                  RecordFile = dataStr,
                                  Magic      = isMagic ? 1 : null,
                                  Cache      = useCache ? 1 : null,
                                  Proxy      = useProxy ? 1 : null,
                                  Timeout    = timeout
                              });
    }

    /// <summary>
    /// 图片 消息段
    /// </summary>
    /// <param name="data">图片名/绝对路径/URL/base64</param>
    /// <param name="useCache">通过URL发送时有效,是否使用已缓存的文件</param>
    /// <param name="threadCount">通过URL发送时有效,通过网络下载图片时的线程数,默认单线程</param>
    /// <remarks>若要使用Base64,需要让 data 以 base64:// 开头</remarks>
    public static SoraSegment Image(string data, bool useCache = true, int? threadCount = null)
    {
        if (string.IsNullOrEmpty(data))
            return IllegalSegment("Image", "data为空");
        (string dataStr, bool isDataStr) = SegmentHelper.ParseDataStr(data);
        return !isDataStr
            ? IllegalSegment("Image", $"非法数据字符串({data})")
            : new SoraSegment(SegmentType.Image,
                              new ImageSegment
                              {
                                  ImgFile     = dataStr,
                                  ImgType     = null,
                                  UseCache    = useCache ? 1 : null,
                                  ThreadCount = threadCount
                              });
    }

    /// <summary>
    /// 图片 消息段
    /// </summary>
    /// <param name="data">图片流</param>
    public static SoraSegment Image(Stream data)
    {
        return data is null
            ? IllegalSegment("Image", "data为空")
            : Image(data.StreamToBase64());
    }

    /// <summary>
    /// 闪照 消息段
    /// </summary>
    /// <param name="data">图片名/绝对路径/URL/base64</param>
    /// <param name="useCache">通过URL发送时有效,是否使用已缓存的文件</param>
    /// <param name="threadCount">通过URL发送时有效,通过网络下载图片时的线程数,默认单线程</param>
    /// <remarks>若要使用Base64,需要让 data 以 base64:// 开头</remarks>
    public static SoraSegment FlashImage(string data, bool useCache = true, int? threadCount = null)
    {
        if (string.IsNullOrEmpty(data))
            return IllegalSegment("Image", "data为空");
        (string dataStr, bool isDataStr) = SegmentHelper.ParseDataStr(data);
        return !isDataStr
            ? IllegalSegment("Image", $"非法数据字符串({data})")
            : new SoraSegment(SegmentType.Image,
                              new ImageSegment
                              {
                                  ImgFile     = dataStr,
                                  ImgType     = "flash",
                                  UseCache    = useCache ? 1 : null,
                                  ThreadCount = threadCount
                              });
    }

    /// <summary>
    /// 秀图 消息段
    /// </summary>
    /// <param name="data">图片名/绝对路径/URL/base64</param>
    /// <param name="useCache">通过URL发送时有效,是否使用已缓存的文件</param>
    /// <param name="threadCount">通过URL发送时有效,通过网络下载图片时的线程数,默认单线程</param>
    /// <param name="id">秀图特效id，默认为40000</param>
    /// <remarks>若要使用Base64,需要让 data 以 base64:// 开头</remarks>
    public static SoraSegment ShowImage(string data, int id = 40000, bool useCache = true, int? threadCount = null)
    {
        if (string.IsNullOrEmpty(data))
            return IllegalSegment("Image", "data为空");
        (string dataStr, bool isDataStr) = SegmentHelper.ParseDataStr(data);
        return !isDataStr
            ? IllegalSegment("ShowImage", $"非法数据字符串({data})")
            : new SoraSegment(SegmentType.Image,
                              new ImageSegment
                              {
                                  ImgFile     = dataStr,
                                  ImgType     = "show",
                                  UseCache    = useCache ? 1 : null,
                                  Id          = id,
                                  ThreadCount = threadCount
                              });
    }

    /// <summary>
    /// 视频 消息段
    /// </summary>
    /// <param name="data">视频名/绝对路径/URL/base64</param>
    /// <param name="useCache">是否使用已缓存的文件</param>
    /// <param name="useProxy">是否通过代理下载文件</param>
    /// <param name="timeout">超时时间，默认为 <see langword="null"/>(不超时)</param>
    /// <remarks>若要使用Base64,需要让 data 以 base64:// 开头</remarks>
    public static SoraSegment Video(string data, bool useCache = true, bool useProxy = true, int? timeout = null)
    {
        (string dataStr, bool isDataStr) = SegmentHelper.ParseDataStr(data);
        return !isDataStr
            ? IllegalSegment("Video", $"非法数据字符串({data})")
            : new SoraSegment(SegmentType.Video,
                              new VideoSegment
                              {
                                  VideoFile = dataStr,
                                  Cache     = useCache ? 1 : null,
                                  Proxy     = useProxy ? 1 : null,
                                  Timeout   = timeout
                              });
    }

    /// <summary>
    /// 音乐 消息段
    /// </summary>
    /// <param name="musicType">音乐分享类型</param>
    /// <param name="musicId">音乐Id</param>
    public static SoraSegment Music(MusicShareType musicType, long musicId)
    {
        return new SoraSegment(SegmentType.Music,
                               new MusicSegment
                               {
                                   MusicType = musicType,
                                   MusicId   = musicId
                               });
    }

    /// <summary>
    /// 自定义音乐分享 消息段
    /// </summary>
    /// <param name="url">跳转URL</param>
    /// <param name="musicUrl">音乐URL</param>
    /// <param name="title">标题</param>
    /// <param name="content">内容描述[可选]</param>
    /// <param name="coverImageUrl">分享内容图片[可选]</param>
    public static SoraSegment CustomMusic(string url,
                                          string musicUrl,
                                          string title,
                                          string content       = null,
                                          string coverImageUrl = null)
    {
        return url is null || musicUrl is null || title is null
            ? IllegalSegment("CustomMusic", "参数不能为空")
            : new SoraSegment(SegmentType.Music,
                              new CustomMusicSegment
                              {
                                  ShareType     = "custom",
                                  Url           = url,
                                  MusicUrl      = musicUrl,
                                  Title         = title,
                                  Content       = content,
                                  CoverImageUrl = coverImageUrl
                              });
    }

    /// <summary>
    /// 链接分享
    /// </summary>
    /// <param name="url">URL</param>
    /// <param name="title">标题</param>
    /// <param name="content">可选，内容描述</param>
    /// <param name="imageUrl">可选，图片 URL</param>
    public static SoraSegment Share(string url, string title, string content = null, string imageUrl = null)
    {
        return url is null || title is null
            ? IllegalSegment("CustomMusic", "参数不能为空")
            : new SoraSegment(SegmentType.Share,
                              new ShareSegment
                              {
                                  Url      = url,
                                  Title    = title,
                                  Content  = content,
                                  ImageUrl = imageUrl
                              });
    }

    /// <summary>
    /// 回复
    /// </summary>
    /// <param name="id">消息id</param>
    public static SoraSegment Reply(int id)
    {
        return new SoraSegment(SegmentType.Reply, new ReplySegment { Target = id });
    }

    /// <summary>
    /// 自定义回复 消息段
    /// </summary>
    /// <param name="text">自定义回复的信息</param>
    /// <param name="uid">自定义回复时的自定义QQ</param>
    /// <param name="time">自定义回复时的时间</param>
    /// <param name="messageSequence">起始消息序号</param>
    public static SoraSegment Reply(string text, long uid, DateTime time, long messageSequence)
    {
        if (text == null)
            return IllegalSegment("Reply", "信息不能为空");
        if (messageSequence <= 0)
            return IllegalSegment("Reply", $"messageSequence超出范围限制({messageSequence})");
        if (uid < 10000)
            return IllegalSegment("Reply", $"uid超出范围限制({uid})");
        return new SoraSegment(SegmentType.Reply,
                               new CustomReplySegment
                               {
                                   Text            = text,
                                   Uid             = uid,
                                   Time            = time,
                                   MessageSequence = messageSequence
                               });
    }

#region GoCQ扩展 消息段

    /// <summary>
    /// <para>群成员戳一戳 消息段</para>
    /// <para>只支持GoCQ</para>
    /// </summary>
    /// <param name="uid">ID</param>
    public static SoraSegment Poke(long uid)
    {
        return uid < 10000
            ? IllegalSegment("Poke", $"uid超出范围限制({uid})")
            : new SoraSegment(SegmentType.Poke, new PokeSegment { Uid = uid });
    }

    /// <summary>
    /// <para>接收红包 消息段</para>
    /// <para>只支持GoCQ</para>
    /// </summary>
    /// <param name="title">祝福语/口令</param>
    public static SoraSegment Redbag(string title)
    {
        return string.IsNullOrEmpty(title)
            ? IllegalSegment("Redbag", "title为空")
            : new SoraSegment(SegmentType.RedBag, new RedbagSegment { Title = title });
    }

    /// <summary>
    /// XML 特殊消息
    /// </summary>
    /// <param name="content">xml文本</param>
    public static SoraSegment Xml(string content)
    {
        return string.IsNullOrEmpty(content)
            ? IllegalSegment("Xml", "content为空")
            : new SoraSegment(SegmentType.Xml,
                              new CodeSegment
                              {
                                  Content = content,
                                  Resid   = null
                              });
    }

    /// <summary>
    /// JSON 特殊消息
    /// </summary>
    /// <param name="content">JSON 文本</param>
    /// <param name="richText">富文本内容</param>
    public static SoraSegment Json(string content, bool richText = false)
    {
        return string.IsNullOrEmpty(content)
            ? IllegalSegment("Json", "content为空")
            : new SoraSegment(SegmentType.Json,
                              new CodeSegment
                              {
                                  Content = content,
                                  Resid   = richText ? 1 : null
                              });
    }

    /// <summary>
    /// JSON 特殊消息
    /// </summary>
    /// <param name="content">JObject实例</param>
    /// <param name="richText">富文本内容</param>
    public static SoraSegment Json(JObject content, bool richText = false)
    {
        return content == null
            ? IllegalSegment("Json", "content为空")
            : new SoraSegment(SegmentType.Json,
                              new CodeSegment
                              {
                                  Content =
                                      JsonConvert.SerializeObject(content, Formatting.None),
                                  Resid = richText ? 1 : null
                              });
    }

    /// <summary>
    /// 装逼大图
    /// </summary>
    /// <param name="imageFile">图片名/绝对路径/URL/base64</param>
    /// <param name="source">来源名称</param>
    /// <param name="iconUrl">来源图标 URL</param>
    /// <param name="minWidth">最小 Width</param>
    /// <param name="minHeight">最小 Height</param>
    /// <param name="maxWidth">最大 Width</param>
    /// <param name="maxHeight">最大 Height</param>
    /// <remarks>若要使用Base64,需要让 imageFile 以 base64:// 开头</remarks>
    public static SoraSegment CardImage(string imageFile,
                                        string source    = null,
                                        string iconUrl   = null,
                                        long   minWidth  = 400,
                                        long   minHeight = 400,
                                        long   maxWidth  = 400,
                                        long   maxHeight = 400)
    {
        if (string.IsNullOrEmpty(imageFile))
            return IllegalSegment("CardImage", "imageFile为空");
        (string dataStr, bool isDataStr) = SegmentHelper.ParseDataStr(imageFile);
        return !isDataStr
            ? IllegalSegment("CardImage", $"非法数据字符串({imageFile})")
            : new SoraSegment(SegmentType.CardImage,
                              new CardImageSegment
                              {
                                  ImageFile = dataStr,
                                  Source    = source,
                                  Icon      = iconUrl,
                                  MinWidth  = minWidth,
                                  MinHeight = minHeight,
                                  MaxWidth  = maxWidth,
                                  MaxHeight = maxHeight
                              });
    }

    /// <summary>
    /// 语音转文字（TTS）消息段
    /// </summary>
    /// <param name="messageStr">要转换的文本信息</param>
    public static SoraSegment TTS(string messageStr)
    {
        return string.IsNullOrEmpty(messageStr)
            ? IllegalSegment("TTS", "messageStr为空")
            : new SoraSegment(SegmentType.TTS, new TtsSegment { Content = messageStr });
    }

    /// <summary>
    /// 猜拳
    /// </summary>
    public static SoraSegment RPS()
    {
        return new SoraSegment(SegmentType.RPS, new BaseSegment());
    }

#endregion GoCQ扩展 消息段

    /// <summary>
    /// 空 消息段
    /// <para>当存在非法参数时消息段将被本函数重置</para>
    /// </summary>
    private static SoraSegment IllegalSegment(string waringType, string waring)
    {
        Log.Error($"{waringType} Segment", $"非法消息段[{waring}]，已忽略消息段");
        return new SoraSegment(SegmentType.Ignore, null);
    }

#endregion 消息段生成/处理
}