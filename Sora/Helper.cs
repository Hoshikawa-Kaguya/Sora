using Newtonsoft.Json;
using Sora.Entities.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sora.Attributes;
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
        /// 对列表进行添加 CommandInfo 元素，或如果存在该项的话，忽略
        /// </summary>
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

        /// <summary>
        /// <para>仅用于项目组内部的代码审查</para>
        /// <para>请不要使用此方法</para>
        /// </summary>
        public static void CheckReviewed()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                                    .Single(assem => assem.FullName == Assembly.GetExecutingAssembly().FullName);

            var methods = new List<MemberInfo>();

            assembly.GetTypes()
                    .Where(type => type.IsClass)
                    .Select(type => type.GetMethods())
                    .ToList()
                    .ForEach(array => methods.AddRange(array));

            assembly.GetTypes()
                    .Where(type => type.IsClass)
                    .Select(type => type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                    .ToList()
                    .ForEach(array => methods.AddRange(array));


            var totalMethod = methods.Count;

            var checkedMethod = methods.Where(m => m.IsDefined(typeof(Reviewed), false))
                                       .ToDictionary(m => m, m => m.GetCustomAttribute<Reviewed>());

            var uncheckedMethod = methods.Where(m => !m.IsDefined(typeof(Reviewed), false))
                                         .ToList();

            Log.Debug("Total Method Count", totalMethod);

            Log.Debug("Checked Method",
                      $"\n{string.Join("\n", checkedMethod.Select(m => $"{m.Key.ReflectedType?.FullName}.{m.Key.Name} checked by {m.Value?.Person} {m.Value?.Time}"))}");

            Log.Debug("Unchecked Method Count", uncheckedMethod.Count);
        }
    }
}