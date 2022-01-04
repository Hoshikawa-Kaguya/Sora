using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
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
    public static string Serialize(this MessageBody msg)
    {
        var ret = "";

        foreach (SoraSegment msgSeg in msg) ret += msgSeg.Serialize();
        return ret;
    }

    /// <summary>
    /// 序列化某一个酷Q码
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static string Serialize(this SoraSegment msg)
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
}