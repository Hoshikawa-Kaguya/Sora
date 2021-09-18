using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Entities.MessageSegment.Segment;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using YukariToolBox.FormatLog;

namespace Sora.Entities.MessageSegment
{
    /// <summary>
    /// 消息段构造
    /// </summary>
    public static class SegmentBuilder
    {
        #region CQ码构建方法

        /// <summary>
        /// 纯文本
        /// </summary>
        /// <param name="msg">文本消息</param>
        internal static CQCode<BaseSegment> TextToBase(string msg)
        {
            return new CQCode<BaseSegment>(CQType.Text, new TextSegment { Content = msg });
        }
        
        /// <summary>
        /// 纯文本
        /// </summary>
        /// <param name="msg">文本消息</param>
        public static CQCode<TextSegment> CQText(string msg)
        {
            return new CQCode<TextSegment>(CQType.Text, new TextSegment { Content = msg });
        }

        /// <summary>
        /// At CQ码
        /// </summary>
        /// <param name="uid">用户uid</param>
        public static CQCode<AtSegment> CQAt(long uid)
        {
            if (uid < 10000)
            {
                Log.Error("CQCode|CQAt", $"非法参数，已忽略CQ码[uid超出范围限制({uid})]");
                return CQIllegal<AtSegment>();
            }

            return new CQCode<AtSegment>(CQType.At,
                              new AtSegment { Target = uid.ToString() });
        }

        /// <summary>
        /// At CQ码
        /// </summary>
        /// <param name="uid">用户uid</param>
        /// <param name="name">当在群中找不到此uid的名称时使用的名字</param>
        // TODO Name参数暂时失效等待测试
        public static CQCode<AtSegment> CQAt(long uid, string name)
        {
            if (uid < 10000)
            {
                Log.Error("CQCode|CQAt", $"非法参数，已忽略CQ码[uid超出范围限制({uid})]");
                return CQIllegal<AtSegment>();
            }

            return new CQCode<AtSegment>(CQType.At,
                                         new AtSegment
                                         {
                                             Target = uid.ToString(),
                                             Name   = name
                                         });
        }

        /// <summary>
        /// At全体 CQ码
        /// </summary>
        public static CQCode<AtSegment> CQAtAll()
        {
            return new(CQType.At,
                       new AtSegment { Target = "all" });
        }

        /// <summary>
        /// 表情CQ码
        /// </summary>
        /// <param name="id">表情 ID</param>
        public static CQCode<FaceSegment> CQFace(int id)
        {
            //检查ID合法性
            if (id is < 0 or > 244)
            {
                Log.Error("CQCode|CQFace", $"非法参数，已忽略CQ码[id超出范围限制({id})]");
                return CQIllegal<FaceSegment>();
            }

            return new CQCode<FaceSegment>(CQType.Face,
                                new FaceSegment { Id = id });
        }

        /// <summary>
        /// 语音CQ码
        /// </summary>
        /// <param name="data">文件名/绝对路径/URL/base64</param>
        /// <param name="isMagic">是否为变声</param>
        /// <param name="useCache">是否使用已缓存的文件</param>
        /// <param name="useProxy">是否通过代理下载文件</param>
        /// <param name="timeout">超时时间，默认为<see langword="null"/>(不超时)</param>
        public static CQCode<RecordSegment> CQRecord(string data, bool isMagic = false, bool useCache = true, bool useProxy = true,
                                        int? timeout = null)
        {
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQRecord", $"非法参数({data})，已忽略此CQ码");
                return CQIllegal<RecordSegment>();
            }

            return new CQCode<RecordSegment>(CQType.Record,
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
        /// 图片CQ码
        /// </summary>
        /// <param name="data">图片名/绝对路径/URL/base64</param>
        /// <param name="useCache">通过URL发送时有效,是否使用已缓存的文件</param>
        /// <param name="threadCount">通过URL发送时有效,通过网络下载图片时的线程数,默认单线程</param>
        public static CQCode<ImageSegment> CQImage(string data, bool useCache = true, int? threadCount = null)
        {
            if (string.IsNullOrEmpty(data)) throw new NullReferenceException(nameof(data));
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQImage", $"非法参数({data})，已忽略CQ码");
                return CQIllegal<ImageSegment>();
            }

            return new CQCode<ImageSegment>(CQType.Image,
                                            new ImageSegment
                                            {
                                                ImgFile     = dataStr,
                                                ImgType     = null,
                                                UseCache    = useCache ? 1 : null,
                                                ThreadCount = threadCount
                                            });
        }

        /// <summary>
        /// 闪照CQ码
        /// </summary>
        /// <param name="data">图片名/绝对路径/URL/base64</param>
        /// <param name="useCache">通过URL发送时有效,是否使用已缓存的文件</param>
        /// <param name="threadCount">通过URL发送时有效,通过网络下载图片时的线程数,默认单线程</param>
        public static CQCode<ImageSegment> CQFlashImage(string data, bool useCache = true, int? threadCount = null)
        {
            if (string.IsNullOrEmpty(data)) throw new NullReferenceException(nameof(data));
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQImage", $"非法参数({data})，已忽略CQ码");
                return CQIllegal<ImageSegment>();
            }

            return new CQCode<ImageSegment>(CQType.Image,
                                            new ImageSegment
                                            {
                                                ImgFile     = dataStr,
                                                ImgType     = "flash",
                                                UseCache    = useCache ? 1 : null,
                                                ThreadCount = threadCount
                                            });
        }

        /// <summary>
        /// 秀图CQ码
        /// </summary>
        /// <param name="data">图片名/绝对路径/URL/base64</param>
        /// <param name="useCache">通过URL发送时有效,是否使用已缓存的文件</param>
        /// <param name="threadCount">通过URL发送时有效,通过网络下载图片时的线程数,默认单线程</param>
        /// <param name="id">秀图特效id，默认为40000</param>
        public static CQCode<ImageSegment> CQShowImage(string data, int id = 40000, bool useCache = true, int? threadCount = null)
        {
            if (string.IsNullOrEmpty(data)) throw new NullReferenceException(nameof(data));
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQShowImage", $"非法参数({data})，已忽略CQ码");
                return CQIllegal<ImageSegment>();
            }

            return new CQCode<ImageSegment>(CQType.Image,
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
        /// 视频CQ码
        /// </summary>
        /// <param name="data">视频名/绝对路径/URL/base64</param>
        /// <param name="useCache">是否使用已缓存的文件</param>
        /// <param name="useProxy">是否通过代理下载文件</param>
        /// <param name="timeout">超时时间，默认为<see langword="null"/>(不超时)</param>
        public static CQCode<VideoSegment> CQVideo(string data, bool useCache = true, bool useProxy = true, int? timeout = null)
        {
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQVideo", $"非法参数({data})，已忽略CQ码");
                return CQIllegal<VideoSegment>();
            }

            return new CQCode<VideoSegment>(CQType.Video,
                                            new VideoSegment
                                            {
                                                VideoFile = dataStr,
                                                Cache     = useCache ? 1 : null,
                                                Proxy     = useProxy ? 1 : null,
                                                Timeout   = timeout
                                            });
        }

        /// <summary>
        /// 音乐CQ码
        /// </summary>
        /// <param name="musicType">音乐分享类型</param>
        /// <param name="musicId">音乐Id</param>
        public static CQCode<MusicSegment> CQMusic(MusicShareType musicType, long musicId)
        {
            return new(CQType.Music,
                       new MusicSegment
                       {
                           MusicType = musicType,
                           MusicId   = musicId
                       });
        }

        /// <summary>
        /// 自定义音乐分享CQ码
        /// </summary>
        /// <param name="url">跳转URL</param>
        /// <param name="musicUrl">音乐URL</param>
        /// <param name="title">标题</param>
        /// <param name="content">内容描述[可选]</param>
        /// <param name="coverImageUrl">分享内容图片[可选]</param>
        public static CQCode<CustomMusicSegment> CQCustomMusic(string url, string musicUrl, string title, string content = null,
                                                               string coverImageUrl = null)
        {
            return new(CQType.Music,
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
        public static CQCode<ShareSegment> CQShare(string url,
                                                   string title,
                                                   string content = null,
                                                   string imageUrl = null)
        {
            return new(CQType.Share,
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
        public static CQCode<ReplySegment> CQReply(int id)
        {
            return new(CQType.Reply,
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
        public static CQCode<CustomReplySegment> CQReply(string text, long uid, DateTime time, long messageSequence)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (messageSequence <= 0)
            {
                Log.Error("CQCode|CQAt", $"非法参数，已忽略CQ码[messageSequence超出范围限制({messageSequence})]");
                return CQIllegal<CustomReplySegment>();
            }

            if (uid < 10000)
            {
                Log.Error("CQCode|CQAt", $"非法参数，已忽略CQ码[uid超出范围限制({uid})]");
                return CQIllegal<CustomReplySegment>();
            }

            return new CQCode<CustomReplySegment>(CQType.Reply,
                                                  new CustomReplySegment
                                                  {
                                                      Text            = text,
                                                      Uid             = uid,
                                                      Time            = time,
                                                      MessageSequence = messageSequence
                                                  });
        }

        #region GoCQ扩展码

        /// <summary>
        /// 群成员戳一戳
        /// 只支持Go-CQHttp
        /// </summary>
        /// <param name="uid">ID</param>
        public static CQCode<PokeSegment> CQPoke(long uid)
        {
            if (uid < 10000)
            {
                Log.Error("CQCode|CQPoke", $"非法参数，已忽略CQ码[uid超出范围限制({uid})]");
                return CQIllegal<PokeSegment>();
            }

            return new CQCode<PokeSegment>(CQType.Poke,
                                           new PokeSegment
                                           {
                                               Uid = uid
                                           });
        }

        /// <summary>
        /// 接收红包
        /// </summary>
        /// <param name="title">祝福语/口令</param>
        public static CQCode<RedbagSegment> CQRedbag(string title)
        {
            if (string.IsNullOrEmpty(title)) throw new NullReferenceException(nameof(title));
            return new CQCode<RedbagSegment>(CQType.RedBag,
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
        public static CQCode<GiftSegment> CQGift(int giftId, long target)
        {
            if (giftId is < 0 or > 8 || target < 10000) throw new ArgumentOutOfRangeException(nameof(giftId));
            return new CQCode<GiftSegment>(CQType.Gift,
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
        public static CQCode<CodeSegment> CQXml(string content)
        {
            if (string.IsNullOrEmpty(content)) throw new NullReferenceException(nameof(content));
            return new CQCode<CodeSegment>(CQType.Xml,
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
        public static CQCode<CodeSegment> CQJson(string content, bool richText = false)
        {
            if (string.IsNullOrEmpty(content)) throw new NullReferenceException(nameof(content));
            return new CQCode<CodeSegment>(CQType.Json,
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
        public static CQCode<CodeSegment> CQJson(JObject content, bool richText = false)
        {
            if (content == null) throw new NullReferenceException(nameof(content));
            return new CQCode<CodeSegment>(CQType.Json,
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
        public static CQCode<CardImageSegment> CQCardImage(string imageFile,
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
                Log.Error("CQCode|CQCardImage", $"非法参数({imageFile})，已忽略CQ码");
                return CQIllegal<CardImageSegment>();
            }

            return new CQCode<CardImageSegment>(CQType.CardImage,
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
        /// 语音转文字（TTS）CQ码
        /// </summary>
        /// <param name="messageStr">要转换的文本信息</param>
        public static CQCode<TtsSegment> CQTTS(string messageStr)
        {
            if (string.IsNullOrEmpty(messageStr)) throw new NullReferenceException(nameof(messageStr));
            return new CQCode<TtsSegment>(CQType.TTS,
                                          new TtsSegment
                                          {
                                              Content = messageStr
                                          });
        }

        #endregion

        /// <summary>
        /// 空CQ码
        /// <para>当存在非法参数时CQ码将被本函数重置</para>
        /// </summary>
        private static CQCode<T> CQIllegal<T>() where T : BaseSegment =>
            new(CQType.Ignore, null);

        #endregion

        #region 扩展构建方法

        /// <summary>
        /// 生成AT CQ码
        /// </summary>
        /// <param name="uid">uid</param>
        public static CQCode<AtSegment> ToAt(this long uid)
        {
            return CQAt(uid);
        }

        /// <summary>
        /// 生成AT CQ码
        /// </summary>
        /// <param name="uid">uid</param>
        public static CQCode<AtSegment> ToAt(this int uid)
        {
            return CQAt(uid);
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
                case CQFileType.UnixFile: //linux/osx
                    if (Environment.OSVersion.Platform != PlatformID.Unix   &&
                        Environment.OSVersion.Platform != PlatformID.MacOSX &&
                        !File.Exists(dataStr))
                        return (dataStr, false);
                    else
                        return ($"file:///{dataStr}", true);
                case CQFileType.WinFile: //win
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