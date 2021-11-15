using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Sora.Attributes;
using Sora.Entities.Info.InternalDataInfo;
using YukariToolBox.LightLog;

namespace Sora.Util;

/// <summary>
/// 通用帮助类
/// </summary>
public static class Helper
{
    #region 崩溃提示

    /// <summary>
    /// 友好的崩溃提示(x)
    /// </summary>
    [Reviewed("nidbCN", "2021-03-24 19:31")]
    internal static void FriendlyException(UnhandledExceptionEventArgs args)
    {
        var e = args.ExceptionObject as Exception;

        if (e is JsonSerializationException)
            Log.Error("Sora", "Json反序列化时出现错误，可能是go-cqhttp配置出现问题。请把go-cqhttp配置中的post_message_format从string改为array。");

        Log.UnhandledExceptionLog(args);
    }

    #endregion

    #region 指令实例相关

    /// <summary>
    /// 对列表进行添加 CommandInfo 元素，或如果存在该项的话，忽略
    /// </summary>
    /// <param name="list">要添加元素的列表</param>
    /// <param name="data">要添加的元素</param>
    /// <returns>是否成功添加，若已存在则返回false。</returns>
    [Reviewed("nidbCN", "2021-03-24 19:39")]
    internal static bool AddOrExist(this List<CommandInfo> list, CommandInfo data)
    {
        if (list.Any(i => i.Equals(data))) return false;
        //当有指令表达式相同且优先级相同时，抛出错误
        if (list.Any(i => i.Regex.ArrayEquals(data.Regex) && i.Priority == data.Priority))
            throw new NotSupportedException("Priority cannot be the same value");
        list.Add(data);
        return true;
    }

    #endregion

    #region 网络

    /// <summary>
    /// 检查端口占用
    /// </summary>
    /// <param name="port">端口号</param>
    internal static bool IsPortInUse(uint port)
    {
        return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
                                 .Any(ipEndPoint => ipEndPoint.Port == port);
    }

    #endregion

    #region 小工具

    /// <summary>
    /// 数组元素完全相等判断（引用类型）
    /// </summary>
    /// <param name="arr1">要判断的数组1</param>
    /// <param name="arr2">要判断的数组2</param>
    /// <typeparam name="T">数组中的元素类型</typeparam>
    /// <returns>2个数组是否全等</returns>
    public static bool ArrayEquals<T>(this T[] arr1, T[] arr2) where T : class
    {
        if (arr1?.Length != arr2?.Length
         || arr1 is null     && arr2 is not null
         || arr1 is not null && arr2 is null)
            return false;

        if (arr1 is null && arr2 is null) return true;

        for (var i = 0; i < arr1?.Length; i++)
            if (arr2 is not null && !(arr1[i] is null && arr2[i] is null))
            {
                if (arr1[i] is null || arr2[i] is null) return false;

                if (!arr1[i].Equals(arr2[i])) return false;
            }

        return true;
    }

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>实例</returns>
    public static T CreateInstance<T>()
    {
        return (T) typeof(T).CreateInstance();
    }

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>实例</returns>
    public static object CreateInstance(this Type type)
    {
        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                              .FirstOrDefault(con => con.GetParameters().Length == 0);


        return constructor?.Invoke(null) ?? FormatterServices.GetUninitializedObject(type);
    }

    #endregion
}