using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Sora.Enumeration;
using Sora.Model.CQCode.CQCodeModel;
using Sora.Model.Message;
using Sora.Tool;

namespace Sora.Model.CQCode
{
    /// <summary>
    /// CQ码类
    /// </summary>
    public class CQCode
    {
        #region 属性
        /// <summary>
        /// CQ码类型
        /// </summary>
        public CQFunction Function { get; private set; }

        /// <summary>
        /// CQ码数据实例
        /// </summary>
        internal object CQData { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造CQ码实例
        /// </summary>
        /// <param name="cqFunction">CQ码类型</param>
        /// <param name="dataObj"></param>
        internal CQCode(CQFunction cqFunction, object dataObj)
        {
            this.Function = cqFunction;
            this.CQData   = dataObj;
        }
        #endregion

        #region CQ码构建方法
        /// <summary>
        /// 纯文本
        /// </summary>
        /// <param name="msg">文本消息</param>
        public static CQCode CQText(string msg)
        {
            return new CQCode(CQFunction.Text,
                              new Text {Content = msg});
        }

        /// <summary>
        /// At CQ码
        /// </summary>
        /// <param name="uid">用户uid</param>
        public static CQCode CQAt(long uid)
        {
            if (uid < 100000)
            {
                ConsoleLog.Error("CQCode|CQAt", $"非法参数，已忽略CQ码[uid超出范围限制({uid})]");
                return CQIlleage();
            }
            return new CQCode(CQFunction.At,
                              new At {Traget = uid.ToString()});
        }

        /// <summary>
        /// At全体 CQ码
        /// </summary>
        public static CQCode CQAtAll()
        {
            return new CQCode(CQFunction.At,
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
                ConsoleLog.Error("CQCode|CQFace", $"非法参数，已忽略CQ码[id超出范围限制({id})]");
                return CQIlleage();
            }
            return new CQCode(CQFunction.Face,
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
            (string dataStr, bool isDataStr) = ParseDataStr(data);
            if (!dataStr.EndsWith("amr") || !dataStr.EndsWith("AMR"))
            {
                ConsoleLog.Error("CQCode|CQRecord", "不支持的格式，只支持AMR格式音频文件");
                return CQIlleage();
            }
            if (!isDataStr)
            {
                ConsoleLog.Error("CQCode|CQRecord", $"非法参数({data})，已忽略此CQ码");
                return CQIlleage();
            }
            return new CQCode(CQFunction.Record,
                              new Record
                              {
                                  RecordFile = dataStr,
                                  Magic      = isMagic ? 1 : 0,
                                  Cache      = useCache ? 1 : 0,
                                  Proxy      = useProxy ? 1 : 0,
                                  Timeout    = timeout
                              });
        }

        /// <summary>
        /// 图片CQ码
        /// </summary>
        /// <param name="data">图片名/绝对路径/URL/base64</param>
        /// <param name="isFlash">是否为闪照，默认为<see langword="false"/></param>
        /// <param name="useCache">是否使用已缓存的文件</param>
        /// <param name="useProxy">是否通过代理下载文件</param>
        /// <param name="timeout">超时时间，默认为<see langword="null"/>(不超时)</param>
        public static CQCode CQImage(string data, bool isFlash = false, bool useCache = true, bool useProxy = true,
                                     int? timeout = null)
        {
            (string dataStr, bool isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                ConsoleLog.Error("CQCode|CQRecord", $"非法参数({data})，已忽略CQ码");
                return CQIlleage();
            }
            return new CQCode(CQFunction.Image,
                              new Image
                              {
                                  ImgFile = dataStr,
                                  ImgType = isFlash ? "flash" : string.Empty,
                                  Cache   = useCache ? 1 : 0,
                                  Proxy   = useProxy ? 1 : 0,
                                  Timeout = timeout
                              });
        }

        /// <summary>
        /// 视频CQ码
        /// </summary>
        /// <param name="data">视频名/绝对路径/URL/base64</param>
        /// <param name="useCache">是否使用已缓存的文件</param>
        /// <param name="useProxy">是否通过代理下载文件</param>
        /// <param name="timeout">超时时间，默认为<see langword="null"/>(不超时)</param>
        [Obsolete]
        public static CQCode CQVideo(string data, bool useCache = true, bool useProxy = true, int? timeout = null)
        {
            (string dataStr, bool isDataStr) = ParseDataStr(data);
            if (!isDataStr)
            {
                ConsoleLog.Error("CQCode|CQVideo", $"非法参数({data})，已忽略CQ码");
                return CQIlleage();
            }
            return new CQCode(CQFunction.Video,
                              new Video
                              {
                                  VideoFile = dataStr,
                                  Cache     = useCache ? 1 : 0,
                                  Proxy     = useProxy ? 1 : 0,
                                  Timeout   = timeout
                              });
        }

        /// <summary>
        /// 群成员戳一戳
        /// </summary>
        /// <param name="uid">ID</param>
        public static CQCode CQPoke(long uid)
        {
            if (uid < 100000)
            {
                ConsoleLog.Error("CQCode|CQPoke", $"非法参数，已忽略CQ码[uid超出范围限制({uid})]");
                return CQIlleage();
            }
            return new CQCode(CQFunction.Poke,
                              new Poke
                              {
                                  Uid = uid
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
            return new CQCode(CQFunction.Share,
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
        /// <param name="id"></param>
        public static CQCode CQReply(int id)
        {
            return new CQCode(CQFunction.Reply,
                              new Reply
                              {
                                  Traget = id
                              });
        }

        /// <summary>
        /// 合并转发
        /// </summary>
        /// <param name="forwardId"></param>
        //TODO 不能使用CQ码形式发送，需要使用/send_group_forward_msg
        [Obsolete]
        public static CQCode CQForward(string forwardId)
        {
            return new CQCode(CQFunction.Node,
                              new Forward
                              {
                                  MessageId = forwardId
                              });
        }

        /// <summary>
        /// XML
        /// </summary>
        /// <param name="content"></param>
        public static CQCode CQXml(string content)
        {
            return new CQCode(CQFunction.Xml,
                              new Code
                              {
                                  Content = content
                              });
        }

        /// <summary>
        /// JSON
        /// </summary>
        /// <param name="content"></param>
        public static CQCode CQJson(string content)
        {
            return new CQCode(CQFunction.Json,
                              new Code
                              {
                                  Content = content
                              });
        }

        /// <summary>
        /// 空CQ码构造
        /// 当存在非法参数时CQ码置空
        /// </summary>
        private static CQCode CQIlleage() =>
            new CQCode(CQFunction.Text, new Text{Content = null});
        #endregion

        #region 获取CQ码内容(仅用于序列化)
        public OnebotMessage ToOnebotMessage() => new OnebotMessage
        {
            MsgType = this.Function,
            RawData = JObject.FromObject(this.CQData)
        };
        #endregion

        #region 正则匹配字段
        private static readonly List<Regex> FileRegices = new List<Regex>
        {
            new Regex(@"^[a-zA-Z]:(((\\(?! )[^/:*?<>\""|\\]+)+\\?)|(\\)?)\s*\.[a-zA-Z]+$", RegexOptions.Compiled), //绝对路径
            new Regex(@"^base64:\/\/[\/]?([\da-zA-Z]+[\/+]+)*[\da-zA-Z]+([+=]{1,2}|[\/])?$", RegexOptions.Compiled),//base64
            new Regex(@"^(http|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?$", RegexOptions.Compiled),//网络图片链接
            new Regex(@"^[\w,\s-]+\.[a-zA-Z0-9]+$", RegexOptions.Compiled)//文件名
        };
        #endregion

        #region 私有方法
        /// <summary>
        /// 处理传入数据
        /// </summary>
        /// <param name="dataStr">数据字符串</param>
        /// <returns>
        /// <para><see langword="retStr"/>处理后数据字符串</para>
        /// <para><see langword="isMatch"/>是否为合法数据字符串</para>
        /// </returns>
        private static (string retStr,bool isMatch) ParseDataStr(string dataStr)
        {
            if (string.IsNullOrEmpty(dataStr)) return (null, false);
            bool isMatch = false;
            foreach (Regex regex in FileRegices)
            {
                isMatch |= regex.IsMatch(dataStr);
            }
            //判断是否是文件名
            if (FileRegices[0].IsMatch(dataStr))
            {
                return ($"file:///{dataStr}",true);
            }

            if (!isMatch) return (dataStr, false);
            return (dataStr, true);
        }
        #endregion
    }
}
