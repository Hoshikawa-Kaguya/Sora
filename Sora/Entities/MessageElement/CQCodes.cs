using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Entities.MessageElement.CQModel;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.OnebotInterface;
using YukariToolBox.FormatLog;

namespace Sora.Entities.MessageElement
{
    /// <summary>
    /// 消息段构造
    /// </summary>
    public static class CQCodes
    {
        #region CQ码构建方法

        /// <summary>
        /// 纯文本
        /// </summary>
        /// <param name="msg">文本消息</param>
        public static CQCode CQText(string msg)
        {
            return new(CQType.Text, new Text {Content = msg});
        }

        /// <summary>
        /// At CQ码
        /// </summary>
        /// <param name="uid">用户uid</param>
        public static CQCode CQAt(long uid)
        {
            if (uid < 10000)
            {
                Log.Error("CQCode|CQAt", $"非法参数，已忽略CQ码[uid超出范围限制({uid})]");
                return CQIlleage();
            }

            return new CQCode(CQType.At,
                              new At {Traget = uid.ToString()});
        }

        /// <summary>
        /// At全体 CQ码
        /// </summary>
        public static CQCode CQAtAll()
        {
            return new(CQType.At,
                       new At {Traget = "all"});
        }

        /// <summary>
        /// 表情CQ码
        /// </summary>
        /// <param name="id">表情 ID</param>
        public static CQCode CQFace(int id)
        {
            //检查ID合法性
            if (id is < 0 or > 244)
            {
                Log.Error("CQCode|CQFace", $"非法参数，已忽略CQ码[id超出范围限制({id})]");
                return CQIlleage();
            }

            return new CQCode(CQType.Face,
                              new Face {Id = id});
        }

        /// <summary>
        /// 语音CQ码
        /// </summary>
        /// <param name="data">文件名/绝对路径/URL/base64</param>
        /// <param name="isMagic">是否为变声</param>
        /// <param name="useCache">是否使用已缓存的文件</param>
        /// <param name="useProxy">是否通过代理下载文件</param>
        /// <param name="timeout">超时时间，默认为<see langword="null"/>(不超时)</param>
        public static CQCode CQRecord(string data, bool isMagic = false, bool useCache = true, bool useProxy = true,
                                      int? timeout = null)
        {
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQRecord", $"非法参数({data})，已忽略此CQ码");
                return CQIlleage();
            }

            return new CQCode(CQType.Record,
                              new Record
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
        public static CQCode CQImage(string data, bool useCache = true, int? threadCount = null)
        {
            if (string.IsNullOrEmpty(data)) throw new NullReferenceException(nameof(data));
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQImage", $"非法参数({data})，已忽略CQ码");
                return CQIlleage();
            }

            return new CQCode(CQType.Image,
                              new Image
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
        public static CQCode CQFlashImage(string data, bool useCache = true, int? threadCount = null)
        {
            if (string.IsNullOrEmpty(data)) throw new NullReferenceException(nameof(data));
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQImage", $"非法参数({data})，已忽略CQ码");
                return CQIlleage();
            }

            return new CQCode(CQType.Image,
                              new Image
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
        public static CQCode CQShowImage(string data, int id = 40000, bool useCache = true, int? threadCount = null)
        {
            if (string.IsNullOrEmpty(data)) throw new NullReferenceException(nameof(data));
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQShowImage", $"非法参数({data})，已忽略CQ码");
                return CQIlleage();
            }

            return new CQCode(CQType.Image,
                              new Image
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
        public static CQCode CQVideo(string data, bool useCache = true, bool useProxy = true, int? timeout = null)
        {
            var (dataStr, isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                Log.Error("CQCode|CQVideo", $"非法参数({data})，已忽略CQ码");
                return CQIlleage();
            }

            return new CQCode(CQType.Video,
                              new Video
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
        public static CQCode CQMusic(MusicShareType musicType, long musicId)
        {
            return new(CQType.Music,
                       new Music
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
        public static CQCode CQCustomMusic(string url, string musicUrl, string title, string content = null,
                                           string coverImageUrl = null)
        {
            return new(CQType.Music,
                       new CustomMusic
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
        public static CQCode CQShare(string url,
                                     string title,
                                     string content = null,
                                     string imageUrl = null)
        {
            return new(CQType.Share,
                       new Share
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
        public static CQCode CQReply(int id)
        {
            return new(CQType.Reply,
                       new Reply
                       {
                           Traget = id
                       });
        }

        #region GoCQ扩展码

        /// <summary>
        /// 群成员戳一戳
        /// 只支持Go-CQHttp
        /// </summary>
        /// <param name="uid">ID</param>
        public static CQCode CQPoke(long uid)
        {
            if (uid < 10000)
            {
                Log.Error("CQCode|CQPoke", $"非法参数，已忽略CQ码[uid超出范围限制({uid})]");
                return CQIlleage();
            }

            return new CQCode(CQType.Poke,
                              new Poke
                              {
                                  Uid = uid
                              });
        }

        /// <summary>
        /// 接收红包
        /// </summary>
        /// <param name="title">祝福语/口令</param>
        public static CQCode CQRedbag(string title)
        {
            if (string.IsNullOrEmpty(title)) throw new NullReferenceException(nameof(title));
            return new CQCode(CQType.RedBag,
                              new Redbag
                              {
                                  Title = title
                              });
        }

        /// <summary>
        /// 发送免费礼物
        /// </summary>
        /// <param name="giftId">礼物id</param>
        /// <param name="target">目标uid</param>
        public static CQCode CQGift(int giftId, long target)
        {
            if (giftId is < 0 or > 8 || target < 10000) throw new ArgumentOutOfRangeException(nameof(giftId));
            return new CQCode(CQType.Gift,
                              new Gift
                              {
                                  Target   = target,
                                  GiftType = giftId
                              });
        }

        /// <summary>
        /// XML 特殊消息
        /// </summary>
        /// <param name="content">xml文本</param>
        public static CQCode CQXml(string content)
        {
            if (string.IsNullOrEmpty(content)) throw new NullReferenceException(nameof(content));
            return new CQCode(CQType.Xml,
                              new Code
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
        public static CQCode CQJson(string content, bool richText = false)
        {
            if (string.IsNullOrEmpty(content)) throw new NullReferenceException(nameof(content));
            return new CQCode(CQType.Json,
                              new Code
                              {
                                  Content = content,
                                  Resid   = richText ? (int?) 1 : null
                              });
        }

        /// <summary>
        /// JSON 特殊消息
        /// </summary>
        /// <param name="content">JObject实例</param>
        public static CQCode CQJson(JObject content)
        {
            if (content == null) throw new NullReferenceException(nameof(content));
            return new CQCode(CQType.Json,
                              new Code
                              {
                                  Content = JsonConvert.SerializeObject(content, Formatting.None)
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
        public static CQCode CQCardImage(string imageFile,
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
                return CQIlleage();
            }

            return new CQCode(CQType.CardImage,
                              new CardImage
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
        /// <returns></returns>
        public static CQCode CQTTS(string messageStr)
        {
            if (string.IsNullOrEmpty(messageStr)) throw new NullReferenceException(nameof(messageStr));
            return new CQCode(CQType.TTS,
                              new
                              {
                                  text = messageStr
                              });
        }

        #endregion

        /// <summary>
        /// 空CQ码
        /// <para>当存在非法参数时CQ码将被本函数重置</para>
        /// </summary>
        private static CQCode CQIlleage() =>
            new(CQType.Text, new Text {Content = null});

        #endregion

        #region 扩展构建方法

        /// <summary>
        /// 生成AT CQ码
        /// </summary>
        /// <param name="uid">uid</param>
        public static CQCode At(this long uid)
        {
            return CQAt(uid);
        }

        /// <summary>
        /// 生成AT CQ码
        /// </summary>
        /// <param name="uid">uid</param>
        public static CQCode At(this int uid)
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