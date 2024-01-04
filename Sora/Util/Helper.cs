using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Sora.Attributes;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Net.Records;
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
    internal static void FriendlyException(Exception e)
    {
        if (e is JsonSerializationException)
        {
            Log.Error("Sora", "Json反序列化时出现错误，可能是go-cqhttp配置出现问题。请把go-cqhttp配置中的post_message_format从string改为array。");
            return;
        }

        Log.Error(e, "Sora", "发生未知错误");
        throw e;
    }

#endregion

#region 指令优先级转换

    internal static List<(int p, List<T> cmds)> ToPriorityList<T>(this List<T> cmdList) where T : BaseCommandInfo
    {
        HashSet<int> priorityRecord = new();
        foreach (T cmd in cmdList)
            priorityRecord.Add(cmd.Priority);

        //将相同优先级的指令顺序打乱
        List<(int p, List<T> cmds)> ret = new();
        foreach (int p in priorityRecord)
        {
            List<T> cmds = cmdList.Where(c => c.Priority == p).ToList();
            int     n    = cmds.Count;

            while (n > 1)
            {
                n--;
                int k = Random.Shared.Next(n + 1);
                (cmds[k], cmds[n]) = (cmds[n], cmds[k]);
            }

            ret.Add((p, cmds));
        }

        return ret;
    }

#endregion

#region 小工具

    /// <summary>
    /// 对列表进行添加元素，或如果存在该项的话，忽略
    /// </summary>
    /// <param name="list">要添加元素的列表</param>
    /// <param name="data">要添加的元素</param>
    /// <returns>是否成功添加，若已存在则返回false。</returns>
    [Reviewed("nidbCN", "2021-03-24 19:39")]
    internal static bool AddOrExist<T>(this List<T> list, T data) where T : BaseCommandInfo
    {
        if (list.Contains(data))
            return false;
        list.Add(data);
        return true;
    }

    /// <summary>
    /// 清除服务数据
    /// </summary>
    /// <param name="serviceId">服务标识</param>
    internal static void DisposeService(Guid serviceId)
    {
        Log.Debug("Sora", "Detect service dispose, cleanup service config...");
        //清空服务信息
        WaitCommandRecord.DisposeSession(serviceId);
        ServiceRecord.RemoveRecord(serviceId);
        Log.Debug("Sora", "Service config cleanup finished");
    }

    /// <summary>
    /// 数组元素完全相等判断（引用类型）
    /// </summary>
    /// <param name="arr1">要判断的数组1</param>
    /// <param name="arr2">要判断的数组2</param>
    /// <typeparam name="T">数组中的元素类型</typeparam>
    /// <returns>2个数组是否全等</returns>
    public static bool ArrayEquals<T>(this T[] arr1, T[] arr2) where T : class
    {
        if (arr1?.Length != arr2?.Length || (arr1 is null && arr2 is not null) || (arr1 is not null && arr2 is null))
            return false;

        if (arr1 is null && arr2 is null)
            return true;

        for (int i = 0; i < arr1!.Length; i++)
            if (arr2 is not null && !(arr1[i] is null && arr2[i] is null))
            {
                if (arr1[i] is null || arr2[i] is null)
                    return false;
                if (!arr1[i].Equals(arr2[i]) && typeof(T) != typeof(Regex))
                    return false;
                if (arr1[i].ToString() != arr2[i].ToString())
                    return false;
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
        return (T)typeof(T).CreateInstance();
    }

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>实例</returns>
    public static object CreateInstance(this Type type)
    {
        ConstructorInfo constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                          .FirstOrDefault(con => con.GetParameters().Length == 0);


        return constructor?.Invoke(null) ?? FormatterServices.GetUninitializedObject(type);
    }

    /// <summary>
    /// 获取属性的Description特性
    /// </summary>
    internal static string GetFieldDesc<T>(T value)
    {
        FieldInfo fieldInfo = value.GetType().GetField(value.ToString()!);
        if (fieldInfo == null)
            return string.Empty;
        DescriptionAttribute[] attributes =
            (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : string.Empty;
    }

#endregion
}