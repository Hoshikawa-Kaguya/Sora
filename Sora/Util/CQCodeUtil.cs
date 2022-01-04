using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;

namespace Sora.Util;

/// <summary>
/// 原CQ码序列化
/// 该方法由ExerciseBook(https://github.com/ExerciseBook)提供
/// </summary>
public static class CQCodeUtil
{
    /// <summary>
    /// 序列化某一个消息
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static string SerializeMessage(this MessageBody msg)
    {
        var ret = "";

        foreach (SoraSegment msgSeg in msg) ret += msgSeg.SerializeSegment();
        return ret;
    }

    /// <summary>
    /// 序列化某一个酷Q码
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static string SerializeSegment(this SoraSegment msg)
    {
        if (msg.MessageType == SegmentType.Text) return ((TextSegment) msg.Data).Content.CQCodeEncode();

        var ret = new StringBuilder();
        ret.Append("[CQ:");

        FieldInfo messageTypeFieldInfo = msg.MessageType.GetType().GetField(msg.MessageType.ToString());
        if (messageTypeFieldInfo == null) return "";
        var attributes =
            (DescriptionAttribute[]) messageTypeFieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attributes.Length < 1) return "";

        string description = attributes[0].Description;
        ret.Append(description);


        BaseSegment    data       = msg.Data;
        Type           dataType   = data.GetType();
        PropertyInfo[] dataFields = dataType.GetProperties();

        foreach (PropertyInfo field in dataFields)
        {
            List<JsonPropertyAttribute> jsonPropertyArr =
                field.GetCustomAttributes<JsonPropertyAttribute>(true).ToList();
            if (jsonPropertyArr.Count != 1) continue;
            JsonPropertyAttribute jsonProperty = jsonPropertyArr.First();
            string                key          = jsonProperty.PropertyName;
            object                propData     = field.GetValue(data);
            if (string.IsNullOrWhiteSpace(key) || propData == null) continue;
            string value = (propData.ToString() ?? "").CQCodeEncode(true);
            ret.Append(',').Append(key).Append('=').Append(value);
        }

        ret.Append(']');
        return ret.ToString();
    }

    #region 反序列化

    private static readonly Regex CQRegex = 
        new(@"\[CQ:([A-Za-z]*)(?:(,[^\[\]]+))?\]", RegexOptions.Compiled);

    private static readonly Regex CQKeyValueRegex = 
        new(@",([A-Za-z]+)=([^,\[\]]+)", RegexOptions.Compiled);

    /// <summary>
    /// 将字符串反序列化为SoraSegment集合
    /// </summary>
    /// <param name="cqMsgStr">需要进行反序列化的消息字符串</param>
    /// <returns>SoraSegment集合</returns>
    public static MessageBody DeserializeMessage(string cqMsgStr)
    {
        // 分离为文本数组和CQ码数组
        string[] text     = CQRegex.Replace(cqMsgStr, "\0").Split('\0');
        Match[]  code     = CQRegex.Matches(cqMsgStr).ToArray();
        var      segments = new List<SoraSegment>();
        for (var i = 0; i < code.Length; i++)
        {
            if (text[i].Length > 0) segments.Add(SoraSegment.Text(text[i]));
            segments.Add(DeserializeCqCode(code[i].Value));
        }

        if (text[code.Length].Length > 0) segments.Add(SoraSegment.Text(text[code.Length]));
        return new MessageBody(segments);
    }

    /// <summary>
    /// 将CQ码反序列化为SoraSegment对象
    /// </summary>
    /// <param name="str">CQ码字符串</param>
    /// <returns>生成的SoraSegment对象</returns>
    public static SoraSegment DeserializeCqCode(string str)
    {
        Match match = CQRegex.Match(str);
        if (!Enum.TryParse(match.Groups[1].Value, true, out SegmentType segmentType))
            segmentType = SegmentType.Unknown;
        MatchCollection collection = CQKeyValueRegex.Matches(match.Groups[2].Value);

        var sb = new StringBuilder();
        sb.Append('{');
        foreach (Match code in collection)
        {
            sb.Append('"');
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

    #endregion

    #region 转义

    /// <summary>
    ///  酷Q码转义
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="comma"></param>
    /// <returns></returns>
    public static string CQCodeEncode(this string msg, bool comma = false)
    {
        var ret = new StringBuilder(255);
        foreach (char t in msg)
            ret.Append(
                t switch
                {
                    '&' => "&amp;",
                    '[' => "&#91;",
                    ']' => "&#93;",
                    ',' => comma ? "&#44;" : ",",
                    _   => t
                }
            );

        return ret.ToString();
    }


    /// <summary>
    /// 需要被反转义的内容
    /// </summary>
    private static readonly string[] DecodeTarget =
    {
        "&amp;", "&#91;", "&#93;", "&#44;"
    };

    /// <summary>
    ///  酷Q码反转义
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static string CQCodeDecode(this string msg)
    {
        var ret = new StringBuilder(255);

        var i    = 0;
        var last = 0;
        while (i < msg.Length)
            // i i+1 i+2 i+3  i+4
            // & a   m   p    ;
            if (msg[i] == '&')
            {
                if (i + 4 <= msg.Length && DecodeTarget.Contains(msg[new Range(i, i + 5)]))
                {
                    string t = msg[new Range(i, i + 5)];
                    char unEscaped = t switch
                    {
                        "&amp;" => '&',
                        "&#91;" => '[',
                        "&#93;" => ']',
                        "&#44;" => ',',
                        _       => throw new ArgumentOutOfRangeException() // unreachable
                    };

                    ret.Append(msg[new Range(last, i)]);
                    ret.Append(unEscaped);

                    i    += 5;
                    last =  i;
                }
                else
                {
                    i++;
                }
            }
            else
            {
                i++;
            }

        if (last < i) ret.Append(msg[new Range(last, i)]);

        return ret.ToString();
    }

    #endregion
}