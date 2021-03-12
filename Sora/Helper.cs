using Newtonsoft.Json;
using Sora.Entities.Info;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using YukariToolBox.FormatLog;

namespace Sora
{
    /// <summary>
    /// 通用帮助类
    /// </summary>
    public static class Helper
    {

        /// <summary>
        /// 友好的崩溃提示(x)
        /// </summary>
        internal static void FriendlyException(UnhandledExceptionEventArgs args)
        {
            var e = args.ExceptionObject as Exception;
            if (e is JsonSerializationException)
            {
                Log.Error("Sora", "Json反序列化时出现错误，可能是go-cqhttp配置出现问题。请把go-cqhttp配置中的post_message_format从string改为array。");
            }

            Log.UnhandledExceptionLog(args);
        }

        /// <summary>
        /// 忽略构造方法并创建实例
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns>实例</returns>
        public static T CreateInstance<T>()
        {
            return (T) FormatterServices.GetUninitializedObject(typeof(T));
        }

        /// <summary>
        /// 忽略构造方法并创建实例
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>实例</returns>
        public static object CreateInstance(this Type type)
        {
            return FormatterServices.GetUninitializedObject(type);
        }

        /// <summary>
        /// 字符串类型转换
        /// </summary>
        /// <typeparam name="T">转换类型</typeparam>
        /// <param name="input">需要转换的字符串</param>
        /// <returns>转换值</returns>
        public static T Convert<T>(this string input)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T) converter.ConvertFromString(input);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        /// <summary>
        /// 字符串类型转换
        /// </summary>
        /// <param name="input">需要转换的字符串</param>
        /// <param name="type">转换类型</param>
        /// <returns>转换值</returns>
        public static object Convert(this string input, Type type)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(type);
                return converter.ConvertFromString(input);
            }
            catch (Exception)
            {
                return null;
            }
        }

#nullable enable
        public static bool ArrayEquals<T>(this T[]? arr1, T[]? arr2)
        {
            if (arr1?.Length != arr2?.Length
             || (arr1 is null    && !(arr2 is null))
             || (!(arr1 is null) && arr2 is null))
            {
                return false;
            }

            if (arr1 is null && arr2 is null)
            {
                return true;
            }

            for (int i = 0; i < arr1?.Length; i++)
            {
                if (!(arr1[i] is null && arr2[i] is null))
                {
                    if (arr1[i] is null || arr2[i] is null)
                    {
                        return false;
                    }

                    if (!arr1[i].Equals(arr2[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 对列表进行添加 CommandInfo 元素，或如果存在该项的话，忽略
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">要添加元素的列表</param>
        /// <param name="data">要添加的元素</param>
        /// <returns>是否成功添加，若已存在则返回false。</returns>
        internal static bool AddOrExist(this List<CommandInfo> list, CommandInfo data)
        {
            if (!list.Any(i => i.Equals(data)))
            {
                list.Add(data);
                return true;
            }

            return false;
        }
    }
}