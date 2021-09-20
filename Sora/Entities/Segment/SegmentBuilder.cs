using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using YukariToolBox.FormatLog;

namespace Sora.Entities.Segment
{
    /// <summary>
    /// 消息段构造
    /// </summary>
    public static class SegmentBuilder
    {
        #region 码构建方法

        /// <summary>
        /// 纯文本
        /// </summary>
        /// <param name="msg">文本消息</param>
        internal static SoraSegment TextToBase(string msg)
        {
            return new SoraSegment(SegmentType.Text, new TextSegment { Content = msg });
        }

        /// <summary>
        /// 纯文本
        /// </summary>
        /// <param name="msg">文本消息</param>
        public static SoraSegment Text(string msg)
        {
            return new SoraSegment(SegmentType.Text, new TextSegment { Content = msg });
        }

        /// <summary>
        /// At 码
        /// </summary>
        /// <param name="uid">用户uid</param>
        public static SoraSegment At(long uid)
        {
            if (uid < 10000)
            {
                Log.Error("SoraSegment|At", $"非法参数，已忽略码[uid超出范围限制({uid})]");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.At,
                                   new AtSegment { Target = uid.ToString() });
        }

        /// <summary>
        /// At 码
        /// </summary>
        /// <param name="uid">用户uid</param>
        /// <param name="name">当在群中找不到此uid的名称时使用的名字</param>
        // TODO Name参数暂时失效等待测试
        public static SoraSegment At(long uid, string name)
        {
            if (uid < 10000)
            {
                Log.Error("SoraSegment|At", $"非法参数，已忽略码[uid超出范围限制({uid})]");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.At,
                                   new AtSegment
                                   {
                                       Target = uid.ToString(),
                                       Name   = name
                                   });
        }

        /// <summary>
        /// At全体 码
        /// </summary>
        public static SoraSegment AtAll()
        {
            return new(SegmentType.At,
                       new AtSegment { Target = "all" });
        }

        /// <summary>
        /// 表情码
        /// </summary>
        /// <param name="id">表情 ID</param>
        public static SoraSegment Face(int id)
        {
            //检查ID合法性
            if (id is < 0 or > 244)
            {
                Log.Error("SoraSegment|Face", $"非法参数，已忽略码[id超出范围限制({id})]");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.Face,
                                   new FaceSegment { Id = id });
        }

        /// <summary>
        /// 语音码
        /// </summary>
        /// <param name="data">文件名/绝对路径/URL/base64</param>
        /// <param name="isMagic">是否为变声</param>
        /// <param name="useCache">是否使用已缓存的文件</param>
        /// <param name="useProxy">是否通过代理下载文件</param>
        /// <param name="timeout">超时时间，默认为<see langword="null"/>(不超时)</param>
        public static SoraSegment Record(string data, bool isMagic = false, bool useCache = true,
                                         bool useProxy = true,
                                         int? timeout = null)
        {
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("SoraSegment|Record", $"非法参数({data})，已忽略此码");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.Record,
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
        /// 图片码
        /// </summary>
        /// <param name="data">图片名/绝对路径/URL/base64</param>
        /// <param name="useCache">通过URL发送时有效,是否使用已缓存的文件</param>
        /// <param name="threadCount">通过URL发送时有效,通过网络下载图片时的线程数,默认单线程</param>
        public static SoraSegment Image(string data, bool useCache = true, int? threadCount = null)
        {
            if (string.IsNullOrEmpty(data)) throw new NullReferenceException(nameof(data));
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("SoraSegment|Image", $"非法参数({data})，已忽略码");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.Image,
                                   new ImageSegment
                                   {
                                       ImgFile     = dataStr,
                                       ImgType     = null,
                                       UseCache    = useCache ? 1 : null,
                                       ThreadCount = threadCount
                                   });
        }

        /// <summary>
        /// 闪照码
        /// </summary>
        /// <param name="data">图片名/绝对路径/URL/base64</param>
        /// <param name="useCache">通过URL发送时有效,是否使用已缓存的文件</param>
        /// <param name="threadCount">通过URL发送时有效,通过网络下载图片时的线程数,默认单线程</param>
        public static SoraSegment FlashImage(string data, bool useCache = true, int? threadCount = null)
        {
            if (string.IsNullOrEmpty(data)) throw new NullReferenceException(nameof(data));
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("SoraSegment|Image", $"非法参数({data})，已忽略码");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.Image,
                                   new ImageSegment
                                   {
                                       ImgFile     = dataStr,
                                       ImgType     = "flash",
                                       UseCache    = useCache ? 1 : null,
                                       ThreadCount = threadCount
                                   });
        }

        /// <summary>
        /// 秀图码
        /// </summary>
        /// <param name="data">图片名/绝对路径/URL/base64</param>
        /// <param name="useCache">通过URL发送时有效,是否使用已缓存的文件</param>
        /// <param name="threadCount">通过URL发送时有效,通过网络下载图片时的线程数,默认单线程</param>
        /// <param name="id">秀图特效id，默认为40000</param>
        public static SoraSegment ShowImage(string data, int id = 40000, bool useCache = true,
                                            int? threadCount = null)
        {
            if (string.IsNullOrEmpty(data)) throw new NullReferenceException(nameof(data));
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("SoraSegment|ShowImage", $"非法参数({data})，已忽略码");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.Image,
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
        /// 视频码
        /// </summary>
        /// <param name="data">视频名/绝对路径/URL/base64</param>
        /// <param name="useCache">是否使用已缓存的文件</param>
        /// <param name="useProxy">是否通过代理下载文件</param>
        /// <param name="timeout">超时时间，默认为<see langword="null"/>(不超时)</param>
        public static SoraSegment Video(string data, bool useCache = true, bool useProxy = true,
                                        int? timeout = null)
        {
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("SoraSegment|Video", $"非法参数({data})，已忽略码");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.Video,
                                   new VideoSegment
                                   {
                                       VideoFile = dataStr,
                                       Cache     = useCache ? 1 : null,
                                       Proxy     = useProxy ? 1 : null,
                                       Timeout   = timeout
                                   });
        }

        /// <summary>
        /// 音乐码
        /// </summary>
        /// <param name="musicType">音乐分享类型</param>
        /// <param name="musicId">音乐Id</param>
        public static SoraSegment Music(MusicShareType musicType, long musicId)
        {
            return new(SegmentType.Music,
                       new MusicSegment
                       {
                           MusicType = musicType,
                           MusicId   = musicId
                       });
        }

        /// <summary>
        /// 自定义音乐分享码
        /// </summary>
        /// <param name="url">跳转URL</param>
        /// <param name="musicUrl">音乐URL</param>
        /// <param name="title">标题</param>
        /// <param name="content">内容描述[可选]</param>
        /// <param name="coverImageUrl">分享内容图片[可选]</param>
        public static SoraSegment CustomMusic(string url, string musicUrl, string title,
                                              string content = null,
                                              string coverImageUrl = null)
        {
            return new(SegmentType.Music,
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
        public static SoraSegment Share(string url,
                                        string title,
                                        string content = null,
                                        string imageUrl = null)
        {
            return new(SegmentType.Share,
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
            return new(SegmentType.Reply,
                       new ReplySegment
                       {
                           Target = id
                       });
        }

        /// <summary>
        /// 自定义回复
        /// </summary>
        /// <param name="text">自定义回复的信息</param>
        /// <param name="uid">自定义回复时的自定义QQ</param>
        /// <param name="time">自定义回复时的时间</param>
        /// <param name="messageSequence">起始消息序号</param>
        public static SoraSegment Reply(string text, long uid, DateTime time, long messageSequence)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (messageSequence <= 0)
            {
                Log.Error("SoraSegment|At", $"非法参数，已忽略码[messageSequence超出范围限制({messageSequence})]");
                return IllegalSegment();
            }

            if (uid < 10000)
            {
                Log.Error("SoraSegment|At", $"非法参数，已忽略码[uid超出范围限制({uid})]");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.Reply,
                                   new CustomReplySegment
                                   {
                                       Text            = text,
                                       Uid             = uid,
                                       Time            = time,
                                       MessageSequence = messageSequence
                                   });
        }

        #region Go扩展码

        /// <summary>
        /// 群成员戳一戳
        /// 只支持Go-Http
        /// </summary>
        /// <param name="uid">ID</param>
        public static SoraSegment Poke(long uid)
        {
            if (uid < 10000)
            {
                Log.Error("SoraSegment|Poke", $"非法参数，已忽略码[uid超出范围限制({uid})]");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.Poke,
                                   new PokeSegment
                                   {
                                       Uid = uid
                                   });
        }

        /// <summary>
        /// 接收红包
        /// </summary>
        /// <param name="title">祝福语/口令</param>
        public static SoraSegment Redbag(string title)
        {
            if (string.IsNullOrEmpty(title)) throw new NullReferenceException(nameof(title));
            return new SoraSegment(SegmentType.RedBag,
                                   new RedbagSegment
                                   {
                                       Title = title
                                   });
        }

        /// <summary>
        /// 发送免费礼物
        /// </summary>
        /// <param name="giftId">礼物id</param>
        /// <param name="target">目标uid</param>
        public static SoraSegment Gift(int giftId, long target)
        {
            if (giftId is < 0 or > 8 || target < 10000) throw new ArgumentOutOfRangeException(nameof(giftId));
            return new SoraSegment(SegmentType.Gift,
                                   new GiftSegment
                                   {
                                       Target   = target,
                                       GiftType = giftId
                                   });
        }

        /// <summary>
        /// XML 特殊消息
        /// </summary>
        /// <param name="content">xml文本</param>
        public static SoraSegment Xml(string content)
        {
            if (string.IsNullOrEmpty(content)) throw new NullReferenceException(nameof(content));
            return new SoraSegment(SegmentType.Xml,
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
            if (string.IsNullOrEmpty(content)) throw new NullReferenceException(nameof(content));
            return new SoraSegment(SegmentType.Json,
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
            if (content == null) throw new NullReferenceException(nameof(content));
            return new SoraSegment(SegmentType.Json,
                                   new CodeSegment
                                   {
                                       Content = JsonConvert.SerializeObject(content, Formatting.None),
                                       Resid   = richText ? 1 : null
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
        public static SoraSegment CardImage(string imageFile,
                                            string source = null,
                                            string iconUrl = null,
                                            long minWidth = 400,
                                            long minHeight = 400,
                                            long maxWidth = 400,
                                            long maxHeight = 400)
        {
            if (string.IsNullOrEmpty(imageFile)) throw new NullReferenceException(nameof(imageFile));
            var (dataStr, isDataStr) = ParseDataStr(imageFile);
            if (!isDataStr)
            {
                Log.Error("SoraSegment|CardImage", $"非法参数({imageFile})，已忽略码");
                return IllegalSegment();
            }

            return new SoraSegment(SegmentType.CardImage,
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
        /// 语音转文字（TTS）码
        /// </summary>
        /// <param name="messageStr">要转换的文本信息</param>
        public static SoraSegment TTS(string messageStr)
        {
            if (string.IsNullOrEmpty(messageStr)) throw new NullReferenceException(nameof(messageStr));
            return new SoraSegment(SegmentType.TTS,
                                   new TtsSegment
                                   {
                                       Content = messageStr
                                   });
        }

        #endregion

        /// <summary>
        /// 空码
        /// <para>当存在非法参数时码将被本函数重置</para>
        /// </summary>
        private static SoraSegment IllegalSegment() =>
            new(SegmentType.Ignore, null);

        #endregion

        #region 扩展构建方法

        /// <summary>
        /// 生成AT 码
        /// </summary>
        /// <param name="uid">uid</param>
        public static SoraSegment ToAt(this long uid)
        {
            return At(uid);
        }

        /// <summary>
        /// 生成AT 码
        /// </summary>
        /// <param name="uid">uid</param>
        public static SoraSegment ToAt(this int uid)
        {
            return At(uid);
        }

        #endregion

        #region 消息字符串处理

        /// <summary>
        /// 处理传入数据
        /// </summary>
        /// <param name="dataStr">数据字符串</param>
        /// <returns>
        /// <para><see langword="retStr"/>处理后数据字符串</para>
        /// <para><see langword="isMatch"/>是否为合法数据字符串</para>
        /// </returns>
        internal static (string retStr, bool isMatch) ParseDataStr(string dataStr)
        {
            if (string.IsNullOrEmpty(dataStr)) return (null, false);
            dataStr = dataStr.Replace('\\', '/');
            //当字符串太长时跳过正则检查
            if (dataStr.Length > 1000) return (dataStr, true);

            var type = StaticVariable.FileRegices.Single(i => i.Value.IsMatch(dataStr)).Key;

            switch (type)
            {
                case FileType.UnixFile: //linux/osx
                    if (Environment.OSVersion.Platform != PlatformID.Unix   &&
                        Environment.OSVersion.Platform != PlatformID.MacOSX &&
                        !File.Exists(dataStr))
                        return (dataStr, false);
                    else
                        return ($"file:///{dataStr}", true);
                case FileType.WinFile: //win
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT && File.Exists(dataStr))
                        return ($"file:///{dataStr}", true);
                    else
                        return (dataStr, false);
                default:
                    return (dataStr, true);
            }
        }

        #endregion
    }
}