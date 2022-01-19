using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sora.Attributes;
using Sora.Attributes.Command;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Sora.Command;

/// <summary>
/// 特性指令管理器
/// </summary>
public class CommandManager
{
    #region 属性

    /// <summary>
    /// 指令服务正常运行标识
    /// </summary>
    public bool ServiceIsRunning { get; private set; }

    #endregion

    #region 私有字段

    private readonly List<RegexCommandInfo> _regexCommands = new();

    private readonly Dictionary<Type, dynamic> _instanceDict = new();

    #endregion

    #region 构造方法

    internal CommandManager(Assembly assembly)
    {
        ServiceIsRunning = false;
        MappingCommands(assembly);
    }

    #endregion

    #region 指令执行和注册

    /// <summary>
    /// 自动注册所有指令
    /// </summary>
    /// <param name="assembly">包含指令的程序集</param>
    [Reviewed("XiaoHe321", "2021-03-28 20:45")]
    public void MappingCommands(Assembly assembly)
    {
        if (assembly == null) return;

        //查找所有的指令集
        Dictionary<Type, MethodInfo[]> cmdGroups =
            assembly.GetExportedTypes()
                     //获取指令组
                    .Where(type => type.IsDefined(typeof(CommandGroup), false) && type.IsClass)
                    .Select(type => (type, type.GetMethods()
                                               .Where(method => method.CheckMethodLegality())
                                               .ToArray()))
                    .ToDictionary(methods => methods.type, methods => methods.Item2.ToArray());

        foreach ((Type classType, MethodInfo[] methodInfos) in cmdGroups)
        foreach (MethodInfo methodInfo in methodInfos)
        {
            //生成指令信息
            if (!GenerateCommandInfo(methodInfo, classType, out RegexCommandInfo commandInfo)) continue;
            //添加指令信息
            if (_regexCommands.AddOrExist(commandInfo))
                Log.Debug("Command", $"Registered {commandInfo.SourceFlag} command [{methodInfo.Name}]");
            else
                Log.Warning("CommandManager", "Command exists");
        }

        //增加正则缓存大小
        Regex.CacheSize  += _regexCommands.Count;
        ServiceIsRunning =  true;
    }

    /// <summary>
    /// 处理聊天指令的单次处理器
    /// </summary>
    /// <param name="eventArgs">事件参数</param>
    /// <returns>是否继续处理接下来的消息</returns>
    [NeedReview("L317-L374")]
    internal async ValueTask CommandAdapter(BaseMessageEventArgs eventArgs)
    {
        #region 信号量消息处理

        //处理消息段

        Dictionary<Guid, WaitingInfo> waitingCommand =
            StaticVariable.WaitingDict
                          .Where(command =>
                               WaitingCommandMatch(command, eventArgs))
                          .ToDictionary(
                               i => i.Key,
                               i => i.Value);

        foreach ((Guid key, WaitingInfo _) in waitingCommand)
        {
            //更新等待列表，设置为当前的eventArgs
            WaitingInfo oldInfo = StaticVariable.WaitingDict[key];
            WaitingInfo newInfo = oldInfo;
            newInfo.EventArgs = eventArgs;
            StaticVariable.WaitingDict.TryUpdate(key, newInfo, oldInfo);
            StaticVariable.WaitingDict[key].Semaphore.Set();
        }

        //当前流程已经处理过wait command了。不再继续处理普通command，否则会一次发两条消息，普通消息留到下一次处理
        if (waitingCommand.Count != 0)
        {
            eventArgs.IsContinueEventChain = false;
            return;
        }

        #endregion

        #region 常规指令处理

        //检查指令池
        if (_regexCommands.Count == 0) return;

        List<RegexCommandInfo> matchedCommand =
            _regexCommands.Where(command => RegexCommandMatch(command, eventArgs))
                          .OrderByDescending(p => p.Priority)
                          .ToList();


        //在没有匹配到指令时直接跳转至Event触发
        if (matchedCommand.Count == 0) return;
        await InvokeCommandMethod(matchedCommand, eventArgs);

        #endregion
    }

    /// <summary>
    /// 获取已注册过的实例
    /// </summary>
    /// <param name="instance">实例</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>获取是否成功</returns>
    [Reviewed("XiaoHe321", "2021-03-28 20:45")]
    public bool GetInstance<T>(out T instance)
    {
        if (_instanceDict.Any(type => type.Key == typeof(T)) && _instanceDict[typeof(T)] is T outVal)
        {
            instance = outVal;
            return true;
        }

        instance = default;
        return false;
    }

    private async ValueTask InvokeCommandMethod(List<RegexCommandInfo> matchedCommands, BaseSoraEventArgs eventArgs)
    {
        //遍历匹配到的每个命令
        foreach (RegexCommandInfo commandInfo in matchedCommands)
        {
            //若是群，则判断权限
            if (eventArgs is GroupMessageEventArgs groupEventArgs)
                if (groupEventArgs.SenderInfo.Role < commandInfo.PermissionType)
                {
                    Log.Warning("CommandAdapter",
                        $"成员{groupEventArgs.SenderInfo.UserId}正在尝试执行指令{commandInfo.MethodInfo.Name}");

                    //权限不足，跳过本命令执行
                    continue;
                }

            try
            {
                Log.Debug("CommandAdapter",
                    $"trigger command [{commandInfo.MethodInfo.ReflectedType?.FullName}.{commandInfo.MethodInfo.Name}]");
                Log.Info("CommandAdapter", $"触发指令[{commandInfo.MethodInfo.Name}]");
                //尝试执行指令并判断异步方法
                bool isAsync =
                    commandInfo.MethodInfo.GetCustomAttribute(typeof(AsyncStateMachineAttribute),
                        false) is not null;
                //执行指令方法
                if (isAsync && commandInfo.MethodInfo.ReturnType != typeof(void))
                {
                    Log.Debug("Command", "invoke async command method");
                    await commandInfo.MethodInfo
                                     .Invoke(
                                          commandInfo.InstanceType == null
                                              ? null
                                              : _instanceDict[commandInfo.InstanceType],
                                          new object[] {eventArgs});
                }
                else
                {
                    Log.Debug("Command", "invoke command method");
                    commandInfo.MethodInfo
                               .Invoke(
                                    commandInfo.InstanceType == null
                                        ? null
                                        : _instanceDict[commandInfo.InstanceType],
                                    new object[] {eventArgs});
                }

                return;
            }
            catch (Exception e)
            {
                string errLog = Log.ErrorLogBuilder(e);
                Log.Error("CommandAdapter", errLog);

                var msg = new StringBuilder();
                msg.AppendLine("指令执行错误");
                if (!string.IsNullOrEmpty(commandInfo.Desc))
                    msg.AppendLine($"Description：{commandInfo.Desc}");
                msg.Append(Log.ErrorLogBuilder(e));


                switch (eventArgs)
                {
                    case GroupMessageEventArgs groupMessageArgs:
                    {
                        await groupMessageArgs.Reply(msg.ToString()).RunCatch(err => throw err);
                        break;
                    }
                    case PrivateMessageEventArgs privateMessageArgs:
                    {
                        await privateMessageArgs.Reply(msg.ToString()).RunCatch(err => throw err);
                        break;
                    }
                }

                //检查是否有异常处理
                if (commandInfo.ExceptionHandler is not null)
                    commandInfo.ExceptionHandler(e);
                else throw;
            }
        }
    }

    #endregion

    #region 指令检查和匹配

    //TODO 动态指令重写

    private bool WaitingCommandMatch(KeyValuePair<Guid, WaitingInfo> command,
                                     BaseMessageEventArgs            eventArgs)
    {
        return eventArgs.SourceType switch
        {
            SourceFlag.Group =>
                //判断发起源
                command.Value.SourceFlag == SourceFlag.Group
                //判断来自同一个连接
             && command.Value.ConnectionId == eventArgs.SoraApi.ConnectionId
                //判断来着同一个群
             && command.Value.Source.g == (eventArgs as GroupMessageEventArgs)?.SourceGroup
                //判断来自同一人
             && command.Value.Source.u == eventArgs.Sender
                //匹配
             && command.Value.CommandExpressions.Any(regex => Regex.IsMatch(eventArgs.Message.RawText, regex,
                    RegexOptions.Compiled | command.Value.RegexOptions)),
            SourceFlag.Private => command.Value.SourceFlag == SourceFlag.Private
                //判断来自同一个连接
             && command.Value.ConnectionId == eventArgs.SoraApi.ConnectionId
                //判断来自同一人
             && command.Value.Source.u == eventArgs.Sender
                //匹配指令
             && command.Value.CommandExpressions.Any(regex => Regex.IsMatch(eventArgs.Message.RawText, regex,
                    RegexOptions.Compiled | command.Value.RegexOptions)),
            _ => false
        };
    }

    private bool RegexCommandMatch(RegexCommandInfo     command,
                                   BaseMessageEventArgs eventArgs)
    {
        bool sourceMatch = true;
        switch (command.SourceFlag)
        {
            case SourceFlag.Group:
                var e = eventArgs as GroupMessageEventArgs ??
                    throw new NullReferenceException("event args is null with unknown reason");
                //检查来源群
                if (command.SourceGroups.Length != 0)
                    sourceMatch &= command.SourceGroups.Any(gid => gid == e.SourceGroup);
                //检查来源用户
                if (command.SourceUsers.Length != 0)
                    sourceMatch &= command.SourceUsers.Any(uid => uid == e.Sender);
                break;
            case SourceFlag.Private:
                //检查来源用户
                if (command.SourceUsers.Length != 0)
                    sourceMatch &= command.SourceUsers.Any(uid => uid == eventArgs.Sender);
                break;
            default:
                return false;
        }

        bool isMatch =
            command.SourceFlag == eventArgs.SourceType && //判断同一源
            command.Regex.Any(regex => sourceMatch &&
                Regex.IsMatch( //判断正则表达式
                    eventArgs.Message.RawText,
                    regex,
                    RegexOptions.Compiled | command.RegexOptions));

        string groupName = string.IsNullOrEmpty(command.GroupName)
            ? string.Empty
            : $"({command.GroupName})";

        if (isMatch)
            Log.Debug("CommandMatch", $"match to command [{groupName}{command.MethodInfo.Name}]");
        return isMatch;
    }

    /// <summary>
    /// 检查实例的存在和生成
    /// </summary>
    [Reviewed("XiaoHe321", "2021-03-16 21:07")]
    private bool CheckAndCreateInstance(Type classType)
    {
        //获取类属性
        if (!classType?.IsClass ?? true)
        {
            Log.Error("Command", "method reflected objcet is not a class");
            return false;
        }

        //检查是否已创建过实例
        if (_instanceDict.Any(ins => ins.Key == classType)) return true;

        try
        {
            //创建实例
            object instance = classType.CreateInstance();

            //添加实例
            _instanceDict
               .Add(classType ?? throw new ArgumentNullException(nameof(classType), "get null class type"),
                    instance);
        }
        catch (Exception e)
        {
            Log.Error("Command", $"cannot create instance with error:{Log.ErrorLogBuilder(e)}");
            return false;
        }

        return true;
    }

    #endregion

    #region 指令信息初始化

    /// <summary>
    /// 生成连续对话上下文
    /// </summary>
    /// <param name="sourceUid">消息源UID</param>
    /// <param name="sourceGroup">消息源GID</param>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="sourceFlag">来源标识</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="connectionId">连接标识</param>
    /// <param name="serviceId">服务标识</param>
    /// <exception cref="NullReferenceException">表达式为空时抛出异常</exception>
    internal static WaitingInfo GenerateWaitingCommandInfo(
        long         sourceUid,    long sourceGroup,  string[] cmdExps, MatchType matchType, SourceFlag sourceFlag,
        RegexOptions regexOptions, Guid connectionId, Guid     serviceId)
    {
        if (cmdExps == null || cmdExps.Length == 0) throw new NullReferenceException("cmdExps is empty");
        string[] matchExp = matchType switch
        {
            MatchType.Full => cmdExps
                             .Select(command => $"^{command}$")
                             .ToArray(),
            MatchType.Regex => cmdExps,
            MatchType.KeyWord => cmdExps
                                .Select(command => $"[{command}]+")
                                .ToArray(),
            _ => throw new NotSupportedException("unknown matchtype")
        };

        return new WaitingInfo(new AutoResetEvent(false),
            matchExp,
            serviceId: serviceId,
            connectionId: connectionId,
            source: (sourceUid, sourceGroup),
            sourceFlag: sourceFlag,
            regexOptions: regexOptions);
    }

    /// <summary>
    /// 生成指令信息
    /// </summary>
    /// <param name="method">指令method</param>
    /// <param name="classType">所在实例类型</param>
    /// <param name="regexCommandInfo">指令信息</param>
    private bool GenerateCommandInfo(MethodInfo method, Type classType, out RegexCommandInfo regexCommandInfo)
    {
        //获取指令属性
        RegexCommand commandAttr =
            method.GetCustomAttribute(typeof(RegexCommand)) as RegexCommand ??
            throw new NullReferenceException("command attribute is null with unknown reason");

        Log.Debug("Command", $"Registering command [{method.Name}]");

        //处理指令匹配类型
        MatchType match = commandAttr.MatchType;
        //处理表达式
        string[] matchExp = ParseCommandExps(commandAttr.CommandExpressions, match);

        //检查和创建实例
        //若创建实例失败且方法不是静态的，则返回空白命令信息
        if (!method.IsStatic && !CheckAndCreateInstance(classType))
        {
            regexCommandInfo = Helper.CreateInstance<RegexCommandInfo>();
            return false;
        }

        //创建指令信息
        regexCommandInfo = new RegexCommandInfo(
            commandAttr.Description,
            matchExp,
            classType.Name,
            method,
            commandAttr.PermissionLevel,
            commandAttr.Priority,
            commandAttr.RegexOptions,
            commandAttr.SourceType,
            commandAttr.ExceptionHandler,
            commandAttr.SourceGroups.IsEmpty() ? Array.Empty<long>() : commandAttr.SourceGroups,
            commandAttr.SourceUsers.IsEmpty() ? Array.Empty<long>() : commandAttr.SourceUsers,
            method.IsStatic ? null : classType);

        return true;
    }

    #endregion

    #region 指令表达式处理

    /// <summary>
    /// 处理指令正则表达式
    /// </summary>
    [NeedReview("ALL")]
    private static string[] ParseCommandExps(string[] cmdExps, MatchType matchType)
    {
        return matchType switch
        {
            MatchType.Full => cmdExps
                             .Select(command => $"^{command}$")
                             .ToArray(),
            MatchType.Regex => cmdExps,
            MatchType.KeyWord => cmdExps
                                .Select(command => $"[{command}]+")
                                .ToArray(),
            _ => throw new NotSupportedException("unknown matchtype")
        };
    }

    #endregion
}