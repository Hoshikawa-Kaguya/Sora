using System;
using System.Collections.Concurrent;
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
using Sora.Entities;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.OnebotAdapter;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Sora.Command;

/// <summary>
/// 特性指令管理器
/// </summary>
public sealed class CommandManager
{
    #region 属性

    /// <summary>
    /// 指令服务正常运行标识
    /// </summary>
    public bool ServiceIsRunning { get; private set; }

    /// <summary>
    /// 抛出指令错误
    /// </summary>
    private bool ThrowCommandErr { get; }

    #endregion

    #region 私有字段

    private readonly List<RegexCommandInfo> _regexCommands = new();

    private readonly List<DynamicCommandInfo> _dynamicCommands = new();

    private readonly ConcurrentDictionary<Type, dynamic> _instanceDict = new();

    #endregion

    #region 构造方法

    internal CommandManager(Assembly assembly, bool throwErr)
    {
        ServiceIsRunning = false;
        ThrowCommandErr  = throwErr;
        MappingCommands(assembly);
    }

    #endregion

    #region 指令注册

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
            Log.Debug("Command", $"Registering command [{methodInfo.Name}]");
            //生成指令信息
            if (!GenerateCommandInfo(methodInfo, classType, out RegexCommandInfo commandInfo)) continue;
            //添加指令信息
            if (_regexCommands.AddOrExist(commandInfo))
                Log.Debug("Command", $"Registered {commandInfo.SourceType} command [{methodInfo.Name}]");
            else
                Log.Warning("CommandManager", "Command exists");
        }

        //增加正则缓存大小
        Regex.CacheSize  += _regexCommands.Count;
        ServiceIsRunning =  true;
    }

    /// <summary>
    /// 动态注册指令
    /// </summary>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="groupCommand">指令执行定义</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则选项</param>
    /// <param name="exceptionHandler">异常处理</param>
    /// <param name="memberRole">成员权限限制</param>
    /// <param name="suCommand">机器人管理员限制</param>
    /// <param name="priority">优先级</param>
    /// <param name="sourceGroups">群组限制</param>
    /// <param name="sourceUsers">成员限制</param>
    /// <param name="desc">描述</param>
    public Guid RegisterGroupDynamicCommand(
        string[]          cmdExps, Func<GroupMessageEventArgs, ValueTask> groupCommand,
        MatchType         matchType        = MatchType.Full,
        RegexOptions      regexOptions     = RegexOptions.None,
        Action<Exception> exceptionHandler = null,
        MemberRoleType    memberRole       = MemberRoleType.Member,
        bool              suCommand        = false,
        int               priority         = 0,
        long[]            sourceGroups     = null,
        long[]            sourceUsers      = null,
        string            desc             = "")
    {
        //判断参数合法性
        if (cmdExps is null || cmdExps.Length == 0) throw new NullReferenceException("cmdExps is empty");
        if (groupCommand is null) throw new NullReferenceException($"{nameof(groupCommand)} is null");

        //生成指令信息
        if (!GenerateDynamicCommandInfo(cmdExps, groupCommand, matchType, memberRole, suCommand, priority, sourceGroups
              , sourceUsers, regexOptions, exceptionHandler, desc, out DynamicCommandInfo commandInfo))
            return Guid.Empty;

        //添加指令信息
        if (_dynamicCommands.AddOrExist(commandInfo))
        {
            Log.Debug("Command", $"Registered {commandInfo.SourceType} dynamic command [{commandInfo.CommandId}]");
        }
        else
        {
            Log.Warning("CommandManager", "Command exists");
            return Guid.Empty;
        }

        return commandInfo.CommandId;
    }

    /// <summary>
    /// 动态注册指令
    /// </summary>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="privateCommand">指令执行定义</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则选项</param>
    /// <param name="exceptionHandler">异常处理</param>
    /// <param name="suCommand">机器人管理员限制</param>
    /// <param name="priority">优先级</param>
    /// <param name="sourceUsers">用户限制</param>
    /// <param name="desc">描述</param>
    public Guid RegisterPrivateDynamicCommand(
        string[]          cmdExps, Func<PrivateMessageEventArgs, ValueTask> privateCommand,
        MatchType         matchType        = MatchType.Full,
        RegexOptions      regexOptions     = RegexOptions.None,
        Action<Exception> exceptionHandler = null,
        bool              suCommand        = false,
        int               priority         = 0,
        long[]            sourceUsers      = null,
        string            desc             = "")
    {
        //判断参数合法性
        if (cmdExps is null || cmdExps.Length == 0) throw new NullReferenceException("cmdExps is empty");

        //生成指令信息
        if (!GenerateDynamicCommandInfo(cmdExps, privateCommand, matchType, suCommand, priority, sourceUsers,
                regexOptions, exceptionHandler, desc, out DynamicCommandInfo commandInfo))
            return Guid.Empty;

        //添加指令信息
        if (_dynamicCommands.AddOrExist(commandInfo))
        {
            Log.Debug("Command", $"Registered {commandInfo.SourceType} dynamic command [{commandInfo.CommandId}]");
        }
        else
        {
            Log.Warning("CommandManager", "Command exists");
            return Guid.Empty;
        }

        return commandInfo.CommandId;
    }

    /// <summary>
    /// 删除指定ID的指令
    /// </summary>
    /// <param name="id">指令id</param>
    public bool DeleteDynamicCommand(Guid id)
    {
        return _dynamicCommands.RemoveAll(cmd => cmd.CommandId == id) > 0;
    }

    #endregion

    #region 指令执行

    /// <summary>
    /// 处理聊天指令
    /// </summary>
    /// <param name="eventArgs">事件参数</param>
    /// <returns>是否继续处理接下来的消息</returns>
    [NeedReview("ALL")]
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

        #region 动态指令处理

        //检查指令池
        if (_regexCommands.Count == 0) return;

        List<DynamicCommandInfo> matchedDynamicCommand =
            _dynamicCommands.Where(command => CommandMatch(command, eventArgs))
                            .OrderByDescending(p => p.Priority)
                            .ToList();

        if (matchedDynamicCommand.Count != 0)
            foreach (DynamicCommandInfo commandInfo in matchedDynamicCommand)
                try
                {
                    Log.Debug("CommandAdapter", $"trigger command [{commandInfo.CommandId}]");
                    Log.Info("CommandAdapter", $"触发指令[{commandInfo.CommandId}]");

                    switch (eventArgs.SourceType)
                    {
                        case SourceFlag.Group:
                            await commandInfo.GroupCommand(eventArgs as GroupMessageEventArgs);
                            break;
                        case SourceFlag.Private:
                            await commandInfo.PrivateCommand(eventArgs as PrivateMessageEventArgs);
                            break;
                    }

                    //检测事件触发中断标志
                    if (!eventArgs.IsContinueEventChain) return;
                }
                catch (Exception err)
                {
                    await CommandErrorMessage(err, eventArgs, commandInfo);
                    return;
                }

        #endregion

        #region 常规指令处理

        //检查指令池
        if (_regexCommands.Count == 0) return;

        List<RegexCommandInfo> matchedCommand =
            _regexCommands.Where(command => CommandMatch(command, eventArgs))
                          .OrderByDescending(p => p.Priority)
                          .ToList();


        //在没有匹配到指令时直接跳转至Event触发
        if (matchedCommand.Count == 0) return;

        //遍历匹配到的每个命令
        foreach (RegexCommandInfo commandInfo in matchedCommand)
            try
            {
                Log.Debug("CommandAdapter",
                    $"trigger command [({commandInfo.GroupName}){commandInfo.MethodInfo.ReflectedType?.FullName}.{commandInfo.MethodInfo.Name}]");
                Log.Info("CommandAdapter",
                    $"触发指令[({commandInfo.GroupName}){commandInfo.MethodInfo.Name}]");
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

                //检测事件触发中断标志
                if (!eventArgs.IsContinueEventChain) break;
            }
            catch (Exception err)
            {
                await CommandErrorMessage(err, eventArgs, commandInfo);
            }

        #endregion
    }

    #endregion

    #region 指令检查和匹配

    [NeedReview("ALL")]
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

    [NeedReview("ALL")]
    private bool CommandMatch(BaseCommandInfo      command,
                              BaseMessageEventArgs eventArgs)
    {
        bool preMatch = command.SourceType == eventArgs.SourceType && //判断同一源
            command.Regex.Any(regex =>
                //判断正则表达式
                Regex.IsMatch(eventArgs.Message.RawText,
                    regex,
                    RegexOptions.Compiled | command.RegexOptions));

        if (!preMatch) return false;

        if (command.SuperUserCommand && !eventArgs.IsSuperUser)
        {
            Log.Warning("CommandAdapter",
                $"成员{eventArgs.Sender.Id}正在尝试执行SuperUser指令");
            return false;
        }

        bool sourceMatch = true;
        switch (eventArgs.SourceType)
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
                //判断权限
                if (e.SenderInfo.Role < command.PermissionType && sourceMatch)
                {
                    switch (command)
                    {
                        case RegexCommandInfo regex:
                            Log.Warning("CommandAdapter",
                                $"成员{e.SenderInfo.UserId}[{e.SenderInfo.Role}]正在尝试执行指令{regex.MethodInfo.Name}[{command.PermissionType}]");
                            break;
                        case DynamicCommandInfo dynamic:
                            Log.Warning("CommandAdapter",
                                $"成员{e.SenderInfo.UserId}[{e.SenderInfo.Role}]正在尝试执行指令{dynamic.CommandId}[{command.PermissionType}]");
                            break;
                    }

                    sourceMatch = false;
                }

                break;
            case SourceFlag.Private:
                //检查来源用户
                if (command.SourceUsers.Length != 0)
                    sourceMatch &= command.SourceUsers.Any(uid => uid == eventArgs.Sender);
                break;
            default:
                return false;
        }

        return sourceMatch;
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
            return _instanceDict
               .TryAdd(classType ?? throw new ArgumentNullException(nameof(classType), "get null class type"),
                    instance);
        }
        catch (Exception e)
        {
            Log.Error("Command", $"cannot create instance with error:{Log.ErrorLogBuilder(e)}");
            return false;
        }
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
    [NeedReview("ALL")]
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
    [NeedReview("ALL")]
    private bool GenerateCommandInfo(MethodInfo method, Type classType, out RegexCommandInfo regexCommandInfo)
    {
        //获取指令属性
        SoraCommand commandAttr =
            method.GetCustomAttribute(typeof(SoraCommand)) as SoraCommand ??
            throw new NullReferenceException("command attribute is null with unknown reason");

        //处理表达式
        string[] matchExp = ParseCommandExps(commandAttr.CommandExpressions, commandAttr.MatchType);

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
            commandAttr.SuperUserCommand,
            method.IsStatic ? null : classType);

        return true;
    }

    /// <summary>
    /// 生成动态指令信息
    /// </summary>
    private bool GenerateDynamicCommandInfo(
        string[]               cmdExps, Func<GroupMessageEventArgs, ValueTask> groupCommand,
        MatchType              matchType,
        MemberRoleType         memberRole,
        bool                   suCommand,
        int                    priority,
        long[]                 sourceGroups, long[] sourceUsers,
        RegexOptions           regexOptions,
        Action<Exception>      exceptionHandler,
        string                 desc,
        out DynamicCommandInfo dynamicCommand)
    {
        Guid id = Guid.NewGuid();
        Log.Debug("Command", $"Registering dynamic command [{id}]");

        //处理表达式
        string[] matchExp = ParseCommandExps(cmdExps, matchType);

        //创建指令信息
        dynamicCommand = new DynamicCommandInfo(
            desc, matchExp, memberRole, priority, regexOptions | RegexOptions.Compiled, exceptionHandler,
            sourceGroups ?? Array.Empty<long>(),
            sourceUsers  ?? Array.Empty<long>(),
            groupCommand, id, suCommand);
        return true;
    }

    /// <summary>
    /// 生成动态指令信息
    /// </summary>
    private bool GenerateDynamicCommandInfo(
        string[]               cmdExps, Func<PrivateMessageEventArgs, ValueTask> privateCommand,
        MatchType              matchType,
        bool                   suCommand,
        int                    priority,
        long[]                 sourceUsers,
        RegexOptions           regexOptions,
        Action<Exception>      exceptionHandler,
        string                 desc,
        out DynamicCommandInfo dynamicCommand)
    {
        Guid id = Guid.NewGuid();
        Log.Debug("Command", $"Registering dynamic command [{id}]");

        //处理表达式
        string[] matchExp = ParseCommandExps(cmdExps, matchType);

        //创建指令信息
        dynamicCommand = new DynamicCommandInfo(
            desc, matchExp, priority, regexOptions | RegexOptions.Compiled, exceptionHandler,
            sourceUsers ?? Array.Empty<long>(),
            privateCommand, id, suCommand);
        return true;
    }

    #endregion

    #region 通用工具

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

    /// <summary>
    /// 指令执行错误时的提示
    /// </summary>
    private async ValueTask CommandErrorMessage(Exception       err, BaseMessageEventArgs eventArgs,
                                                BaseCommandInfo commandInfo)
    {
        string errLog = Log.ErrorLogBuilder(err);
        Log.Error("CommandAdapter", errLog);

        var msg = new StringBuilder();
        msg.AppendLine("指令执行错误");
        if (!string.IsNullOrEmpty(commandInfo.Desc))
            msg.AppendLine($"Description：{commandInfo.Desc}");
        msg.Append(Log.ErrorLogBuilder(err));


        switch (eventArgs.SourceType)
        {
            case SourceFlag.Group:
                if (eventArgs is not GroupMessageEventArgs e) break;
                await ApiAdapter.SendGroupMessage(eventArgs.ConnId, e.SourceGroup, msg.ToString(), null)
                                .RunCatch(er =>
                                 {
                                     Log.Error(er, "err cmd", "报错信息发送失败");
                                     return (new ApiStatus(), 0);
                                 });
                break;
            case SourceFlag.Private:
                await ApiAdapter.SendPrivateMessage(eventArgs.ConnId, eventArgs.Sender,
                                     msg.ToString(), null, null)
                                .RunCatch(er =>
                                 {
                                     Log.Error(er, "err cmd", "报错信息发送失败");
                                     return (new ApiStatus(), 0);
                                 });
                break;
        }

        //检查是否有异常处理
        if (commandInfo.ExceptionHandler is not null)
            commandInfo.ExceptionHandler(err);
        else if (ThrowCommandErr) throw err;
    }

    #endregion
}