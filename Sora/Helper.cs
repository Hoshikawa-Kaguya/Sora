using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Sora
{
    /// <summary>
    /// 通用帮助类
    /// </summary>
    public static class Helper
    {
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

        public static void AddOrExist<T>(this List<T> list, T data)
        {
            if (!list.Contains(data))
            {
                list.Add(data);
            }
        }
    }
}