using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sora.Attributes;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Net.Records;
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

    /// <summary>
    /// 在指令出错时向发送源发送报错消息
    /// </summary>
    private bool SendCommandErrMsg { get; }

    /// <summary>
    /// 服务ID
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    private Guid ServiceId { get; }

    /// <summary>
    /// 指令执行错误委托
    /// </summary>
    private Action<Exception, BaseMessageEventArgs, string> CmdExceptionHandler { get; }

#endregion

#region 私有字段

    private readonly List<SoraCommandInfo> _regexCommands = new();

    private readonly List<DynamicCommandInfo> _dynamicCommands = new();

    private readonly ConcurrentDictionary<string, bool> _commandEnableFlagDict = new();

#endregion

#region 构造方法

    internal CommandManager(Assembly                                        assembly,
                            Guid                                            serviceId,
                            bool                                            throwErr,
                            bool                                            sendCommandErrMsg,
                            Action<Exception, BaseMessageEventArgs, string> cmdExceptionHandler)
    {
        ServiceId           = serviceId;
        ServiceIsRunning    = false;
        ThrowCommandErr     = throwErr;
        SendCommandErrMsg   = sendCommandErrMsg;
        CmdExceptionHandler = cmdExceptionHandler;
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
        if (assembly == null)
            return;

        //查找所有的指令集
        Dictionary<Type, MethodInfo[]> cmdSeries =
            assembly.GetExportedTypes()
                    //获取指令组
                    .Where(type => type.IsDefined(typeof(CommandSeries), false)
                                   && type.IsClass)
                    //指令参数方法合法性检查
                    .Select(type => (type, type.GetMethods()
                                               .Where(method => method.CheckCommandMethodLegality()).ToArray()))
                    //将每个类的方法整合
                    .ToDictionary(methods => methods.type,
                                  methods => methods.Item2);

        foreach ((Type classType, MethodInfo[] methodInfos) in cmdSeries)
        {
            //获取指令属性
            CommandSeries seriesAttr =
                classType.GetCustomAttribute(typeof(CommandSeries)) as CommandSeries 
                ?? throw new NullReferenceException("CommandSeries attribute is null with unknown reason");

            string prefix        = string.IsNullOrEmpty(seriesAttr.GroupPrefix) ? string.Empty : seriesAttr.GroupPrefix;
            string seriesName    = string.IsNullOrEmpty(seriesAttr.SeriesName) ? classType.Name : seriesAttr.SeriesName;
            bool   seriesSuccess = false;

            Log.Debug("Command", $"Registering command group[{seriesName}]");
            foreach (MethodInfo methodInfo in methodInfos)
            {
                Log.Debug("Command", $"Registering command [{methodInfo.Name}]");
                //生成指令信息
                if (!GenerateCommandInfo(methodInfo, classType, seriesName, prefix, out SoraCommandInfo commandInfo))
                    continue;
                //添加指令信息
                if (_regexCommands.AddOrExist(commandInfo))
                {
                    seriesSuccess = true;
                    Log.Debug("Command", $"Registered {commandInfo.SourceType} command [{methodInfo.Name}]");
                }
                else
                {
                    Log.Warning("CommandManager", "指令已存在");
                }
            }

            //设置使能字典
            if (seriesSuccess)
                _commandEnableFlagDict.TryAdd(seriesName, true);
        }

        //增加正则缓存大小
        Regex.CacheSize  += _regexCommands.Count;
        ServiceIsRunning =  true;
    }

    /// <summary>
    /// 动态注册指令
    /// </summary>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="seriesCommand">指令执行定义</param>
    /// <param name="sourceType">消息来源范围</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则选项</param>
    /// <param name="groupName">指令组名，为空时不能控制使能</param>
    /// <param name="memberRole">成员权限限制</param>
    /// <param name="suCommand">机器人管理员限制</param>
    /// <param name="priority"><para>优先级</para><para>为<see langword="null"/>时自动设置</para></param>
    /// <param name="sourceGroups">群组限制</param>
    /// <param name="sourceUsers">成员限制</param>
    /// <param name="sourceLogins">限制响应的bot账号来源</param>
    /// <param name="desc">描述</param>
    public Guid RegisterDynamicCommand(string[]                              cmdExps,
                                       Func<BaseMessageEventArgs, ValueTask> seriesCommand,
                                       MessageSourceMatchFlag                sourceType   = MessageSourceMatchFlag.All,
                                       MatchType                             matchType    = MatchType.Full,
                                       RegexOptions                          regexOptions = RegexOptions.None,
                                       string                                groupName    = "",
                                       MemberRoleType                        memberRole   = MemberRoleType.Member,
                                       bool                                  suCommand    = false,
                                       int?                                  priority     = null,
                                       long[]                                sourceGroups = null,
                                       long[]                                sourceUsers  = null,
                                       long[]                                sourceLogins = null,
                                       string                                desc         = "")
    {
        //判断参数合法性
        if (cmdExps is null || cmdExps.Length == 0)
            throw new NullReferenceException("cmdExps is empty");
        if (seriesCommand is null)
            throw new NullReferenceException($"{nameof(seriesCommand)} is null");

        //生成指令信息
        Guid id = Guid.NewGuid();
        Log.Debug("Command", $"Registering dynamic command [{id}]");

        //处理表达式
        string[] matchExp = CommandUtils.ParseCommandExps(cmdExps, string.Empty, matchType);


        //创建指令信息
        DynamicCommandInfo dynamicCommand = new(desc,
                                                matchExp,
                                                null,
                                                memberRole,
                                                priority ?? GetNewDynamicPriority(),
                                                regexOptions | RegexOptions.Compiled,
                                                sourceType,
                                                sourceGroups ?? Array.Empty<long>(),
                                                sourceUsers ?? Array.Empty<long>(),
                                                sourceLogins ?? Array.Empty<long>(),
                                                seriesCommand,
                                                id,
                                                suCommand,
                                                groupName);

        return AddDynamicCommand(dynamicCommand);
    }

    /// <summary>
    /// 动态注册指令
    /// </summary>
    /// <param name="matchFunc">自定义匹配方法</param>
    /// <param name="seriesCommand">指令执行定义</param>
    /// <param name="sourceType">消息来源范围</param>
    /// <param name="groupName">指令组名，为空时不能控制使能</param>
    /// <param name="memberRole">成员权限限制</param>
    /// <param name="suCommand">机器人管理员限制</param>
    /// <param name="priority"><para>优先级</para><para>为<see langword="null"/>时自动设置</para></param>
    /// <param name="sourceGroups">群组限制</param>
    /// <param name="sourceUsers">成员限制</param>
    /// <param name="sourceLogins">限制响应的bot账号来源</param>
    /// <param name="desc">描述</param>
    public Guid RegisterDynamicCommand(Func<BaseMessageEventArgs, bool>      matchFunc,
                                       Func<BaseMessageEventArgs, ValueTask> seriesCommand,
                                       MessageSourceMatchFlag                sourceType   = MessageSourceMatchFlag.All,
                                       string                                groupName    = "",
                                       MemberRoleType                        memberRole   = MemberRoleType.Member,
                                       bool                                  suCommand    = false,
                                       int?                                  priority     = null,
                                       long[]                                sourceGroups = null,
                                       long[]                                sourceUsers  = null,
                                       long[]                                sourceLogins = null,
                                       string                                desc         = "")
    {
        //判断参数合法性
        if (seriesCommand is null)
            throw new NullReferenceException($"{nameof(seriesCommand)} is null");

        //生成指令信息
        Guid id = Guid.NewGuid();
        Log.Debug("Command", $"Registering dynamic command [{id}]");

        //创建指令信息
        DynamicCommandInfo dynamicCommand = new(desc,
                                                Array.Empty<string>(),
                                                matchFunc,
                                                memberRole,
                                                priority ?? GetNewDynamicPriority(),
                                                RegexOptions.None,
                                                sourceType,
                                                sourceGroups ?? Array.Empty<long>(),
                                                sourceUsers ?? Array.Empty<long>(),
                                                sourceLogins ?? Array.Empty<long>(),
                                                seriesCommand,
                                                id,
                                                suCommand,
                                                groupName);

        return AddDynamicCommand(dynamicCommand);
    }

    /// <summary>
    /// 添加新指令到字典
    /// </summary>
    private Guid AddDynamicCommand(DynamicCommandInfo command)
    {
        if (!string.IsNullOrEmpty(command.SeriesName) && _commandEnableFlagDict.TryAdd(command.SeriesName, true))
            Log.Info("DynamicCommand", $"注册新的动态指令组[{command.SeriesName}]");

        //添加指令信息
        if (_dynamicCommands.AddOrExist(command))
        {
            Log.Debug("Command", $"Registered {command.SourceType} dynamic command [{command.CommandId}]");
        }
        else
        {
            Log.Warning("CommandManager", "指令已存在");
            return Guid.Empty;
        }

        return command.CommandId;
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
        List<Guid> waitingCommand = WaitCommandRecord.GetMatchCommand(eventArgs);
        foreach (Guid key in waitingCommand)
            WaitCommandRecord.UpdateRecord(key, eventArgs);

        //当前流程已经处理过wait command了。不再继续处理普通command，否则会一次发两条消息，普通消息留到下一次处理
        if (waitingCommand.Count != 0)
        {
            eventArgs.IsContinueEventChain = false;
            return;
        }

#endregion

#region 动态指令处理

        //检查指令池
        if (_dynamicCommands.Count == 0 && _regexCommands.Count == 0)
            return;

        List<(int, List<DynamicCommandInfo>)> matchedDynamicCommand =
            _dynamicCommands.Where(command => CommandMatch(command, eventArgs))
                            .OrderByDescending(c => c.Priority).ToList()
                            .ToPriorityList();

        foreach ((int p, List<DynamicCommandInfo> commandInfos) in matchedDynamicCommand)
        foreach (DynamicCommandInfo commandInfo in commandInfos)
        {
            Log.Info("CommandAdapter", $"触发指令 [<{p}>{commandInfo.CommandId}]");
            eventArgs.CommandId   = commandInfo.CommandId;
            eventArgs.CommandName = commandInfo.CommandName;

            eventArgs.CommandRegex = commandInfo.Regex.Length != 0
                ? commandInfo.Regex.Where(r => r.IsMatch(eventArgs.Message.RawText)).ToArray()
                : Array.Empty<Regex>();

            //执行动态指令
            try
            {
                await commandInfo.Command(eventArgs);
            }
            catch (Exception err)
            {
                await CommandErrorParse(err, eventArgs, commandInfo, commandInfo.CommandId.ToString());
                return;
            }

            //清空指令参数信息
            eventArgs.CommandId    = Guid.Empty;
            eventArgs.CommandName  = string.Empty;
            eventArgs.CommandRegex = null;

            //检测事件触发中断标志
            if (!eventArgs.IsContinueEventChain)
                return;
        }

#endregion

#region 常规指令处理

        List<(int, List<SoraCommandInfo>)> matchedCommand =
            _regexCommands.Where(c => CommandMatch(c, eventArgs))
                          .OrderByDescending(c => c.Priority).ToList()
                          .ToPriorityList();

        //在没有匹配到指令时直接跳转至Event触发
        if (matchedCommand.Count == 0)
            return;

        //清空指令参数信息
        eventArgs.CommandId = Guid.Empty;

        //遍历匹配到的每个命令
        foreach ((int p, List<SoraCommandInfo> commandInfos) in matchedCommand)
        foreach (SoraCommandInfo commandInfo in commandInfos)
        {
            Log.Info("CommandAdapter",
                     $"触发指令[<{p}>(C:{commandInfo.ClassName}|G:{commandInfo.SeriesName}){commandInfo.MethodInfo.ReflectedType?.FullName}.{commandInfo.MethodInfo.Name}]");
            //尝试执行指令并判断异步方法
            bool isAsync =
                commandInfo.MethodInfo.GetCustomAttribute(typeof(AsyncStateMachineAttribute), false) is not null;

            eventArgs.CommandName = commandInfo.CommandName;
            eventArgs.CommandRegex = commandInfo.Regex.Length != 0
                ? commandInfo.Regex.Where(r => r.IsMatch(eventArgs.Message.RawText)).ToArray()
                : Array.Empty<Regex>();

            //执行指令方法
            Log.Debug("Command", "invoke command method");
            try
            {
                dynamic instance = null;
                if (commandInfo.InstanceType != null)
                    if (!GetInstance(commandInfo.InstanceType, out instance))
                    {
                        Log.Error("Command", $"获取指令实例失败 [t:{commandInfo.InstanceType}]");
                        continue;
                    }

                if (isAsync && commandInfo.MethodInfo.ReturnType != typeof(void))
                    await commandInfo.MethodInfo.Invoke(instance, new object[] { eventArgs });
                else
                    commandInfo.MethodInfo.Invoke(instance, new object[] { eventArgs });
            }
            catch (Exception err)
            {
                await CommandErrorParse(err, eventArgs, commandInfo, commandInfo.MethodInfo.Name);
                return;
            }

            eventArgs.CommandName  = string.Empty;
            eventArgs.CommandRegex = null;

            //检测事件触发中断标志
            if (!eventArgs.IsContinueEventChain)
                break;
        }

#endregion
    }

#endregion

#region 指令检查和匹配

    [NeedReview("ALL")]
    private bool CommandMatch(BaseCommandInfo command, BaseMessageEventArgs eventArgs)
    {
        if (!string.IsNullOrEmpty(command.SeriesName)
            && _commandEnableFlagDict.ContainsKey(command.SeriesName)
            && !_commandEnableFlagDict[command.SeriesName])
            return false;

        //判断bot消息来源
        if (command.SourceLogins.Count != 0 && !command.SourceLogins.Contains(eventArgs.LoginUid))
            return false;

        //判断同一源
        switch (command.SourceType)
        {
            case MessageSourceMatchFlag.All:
                break;
            case MessageSourceMatchFlag.Group:
                if (eventArgs.SourceType != SourceFlag.Group)
                    return false;
                break;
            case MessageSourceMatchFlag.Private:
                if (eventArgs.SourceType != SourceFlag.Private)
                    return false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command.SourceType));
        }

        //判断动态指令的自定义表达式
        if (command is DynamicCommandInfo dynamicCommand)
            if (dynamicCommand.CommandMatchFunc is not null && !dynamicCommand.CommandMatchFunc.Invoke(eventArgs))
                return false;

        //判断所有指令的正则表达式
        if (command.Regex.Length != 0 && !command.Regex.Any(regex => regex.IsMatch(eventArgs.Message.RawText)))
            return false;

        //机器人管理员判断
        if (command.SuperUserCommand && !eventArgs.IsSuperUser)
        {
            Log.Warning("CommandAdapter", $"成员{eventArgs.Sender.Id}正在尝试执行SuperUser指令[{command.CommandName}]");
            return false;
        }

        bool sourceMatch = true;
        switch (eventArgs.SourceType)
        {
            case SourceFlag.Group:
            {
                GroupMessageEventArgs e = eventArgs as GroupMessageEventArgs
                                          ?? throw new NullReferenceException("event args is null with unknown reason");
                //检查来源群
                if (command.SourceGroups.Count != 0)
                    sourceMatch &= command.SourceGroups.Contains(e.SourceGroup.Id);
                //检查群是否禁用过该指令组
                if (ServiceRecord.IsGroupBlockedCommand(eventArgs.ServiceId, e.SourceGroup.Id, command.SeriesName))
                {
                    Log.Info("CommandAdapter", $"群[{e.SourceGroup.Id}]已禁用指令组[{command.SeriesName}]");
                    return false;
                }

                //检查来源用户
                if (command.SourceUsers.Count != 0)
                    sourceMatch &= command.SourceUsers.Contains(e.Sender.Id);
                //判断权限
                if (e.SenderInfo.Role < command.PermissionType && sourceMatch)
                {
                    switch (command)
                    {
                        case SoraCommandInfo regex:
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
            }
            case SourceFlag.Private:
                //检查来源用户
                if (command.SourceUsers.Count != 0)
                    sourceMatch &= command.SourceUsers.Contains(eventArgs.Sender.Id);
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
            Log.Error("Command", $"[{classType?.Name}] 不是一个class");
            return false;
        }

        //检查是否已注册过实例
        if (ServiceHelper.Services.Any(a => a.ServiceType == classType))
            return true;

        Log.Debug("Command", $"reg class:{classType.FullName}");
        //注册实例
        object instance = classType.CreateInstance();
        ServiceHelper.Services.AddSingleton(classType, instance);
        return ServiceHelper.Services.Any(a => a.ServiceType == classType);
    }

#endregion

#region 指令使能

    /// <summary>
    /// 尝试启用指令组
    /// </summary>
    /// <param name="groupName">指令组名</param>
    public bool TryEnableCommandSeries(string groupName)
    {
        return _commandEnableFlagDict.TryUpdate(groupName, true, false);
    }

    /// <summary>
    /// 尝试禁用指令组
    /// </summary>
    /// <param name="groupName">指令组名</param>
    public bool TryDisableCommandSeries(string groupName)
    {
        return _commandEnableFlagDict.TryUpdate(groupName, false, true);
    }

    /// <summary>
    /// 启用群组中禁用的指令
    /// </summary>
    /// <param name="cmdGroupName">指令组名</param>
    /// <param name="groupId">群组ID</param>
    public bool TryEnableGroupCommand(string cmdGroupName, long groupId)
    {
        return ServiceRecord.EnableGroupCommand(ServiceId, cmdGroupName, groupId);
    }

    /// <summary>
    /// 禁用群组中的指令
    /// </summary>
    /// <param name="cmdGroupName">指令组名</param>
    /// <param name="groupId">群组ID</param>
    public bool TryDisableGroupCommand(string cmdGroupName, long groupId)
    {
        return ServiceRecord.DisableGroupCommand(ServiceId, cmdGroupName, groupId);
    }

#endregion

#region 指令信息初始化

    /// <summary>
    /// 生成指令信息
    /// </summary>
    /// <param name="method">指令method</param>
    /// <param name="classType">所在实例类型</param>
    /// <param name="groupName">指令组</param>
    /// <param name="prefix">指令前缀</param>
    /// <param name="soraCommandInfo">指令信息</param>
    [NeedReview("ALL")]
    private bool GenerateCommandInfo(MethodInfo          method,
                                     Type                classType,
                                     string              groupName,
                                     string              prefix,
                                     out SoraCommandInfo soraCommandInfo)
    {
        //获取指令属性
        SoraCommand commandAttr = method.GetCustomAttribute(typeof(SoraCommand)) as SoraCommand
                                  ?? throw new
                                      NullReferenceException("SoraCommand attribute is null with unknown reason");
        //处理表达式
        string[] matchExp =
            CommandUtils.ParseCommandExps(commandAttr.CommandExpressions, prefix, commandAttr.MatchType);

        //检查和创建实例
        //若创建实例失败且方法不是静态的，则返回空白命令信息
        if (!method.IsStatic && !CheckAndCreateInstance(classType))
        {
            soraCommandInfo = Helper.CreateInstance<SoraCommandInfo>();
            return false;
        }

        //创建指令信息
        soraCommandInfo = new SoraCommandInfo(commandAttr.Description,
                                              matchExp,
                                              classType.Name,
                                              groupName,
                                              method,
                                              commandAttr.PermissionLevel,
                                              commandAttr.Priority,
                                              commandAttr.RegexOptions,
                                              commandAttr.SourceType,
                                              commandAttr.SourceGroups.IsEmpty()
                                                  ? Array.Empty<long>()
                                                  : commandAttr.SourceGroups,
                                              commandAttr.SourceUsers.IsEmpty()
                                                  ? Array.Empty<long>()
                                                  : commandAttr.SourceUsers,
                                              commandAttr.SourceLogins.IsEmpty()
                                                  ? Array.Empty<long>()
                                                  : commandAttr.SourceLogins,
                                              commandAttr.SuperUserCommand,
                                              method.IsStatic ? null : classType);

        return true;
    }

#endregion

#region 通用工具

    /// <summary>
    /// 获取指定群中的指令使能列表
    /// </summary>
    /// <param name="groupId">群组ID</param>
    /// <returns>指令使能列表</returns>
    public Dictionary<string, bool> GetGroupCmdSeries(long groupId)
    {
        Dictionary<string, bool> result = new();
        foreach (string sName in _commandEnableFlagDict.Keys)
            result.Add(sName, !ServiceRecord.IsGroupBlockedCommand(ServiceId, groupId, sName));
        return result;
    }

    /// <summary>
    /// 获取当前服务下的所有指令组名
    /// </summary>
    public List<string> GetCommandList()
    {
        return _commandEnableFlagDict.Keys.ToList();
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
        instance = ServiceHelper.GetService<T>();
        return instance is not null;
    }

    /// <summary>
    /// 获取已注册过的实例
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="type">Type</param>
    /// <returns>获取是否成功</returns>
    public bool GetInstance(Type type, out dynamic instance)
    {
        instance = ServiceHelper.GetService(type);
        return instance is not null;
    }

    /// <summary>
    /// 指令执行错误时的处理
    /// </summary>
    private async ValueTask CommandErrorParse(Exception            err,
                                              BaseMessageEventArgs eventArgs,
                                              BaseCommandInfo      commandInfo,
                                              string               cmdName)
    {
        string        errLog = Log.ErrorLogBuilder(err);
        StringBuilder msg    = new();
        msg.AppendLine("指令执行错误");
        msg.AppendLine($"指令：{cmdName}");
        msg.AppendLine($"MsgID:{eventArgs.Message.MessageId}");
        if (!string.IsNullOrEmpty(commandInfo.Desc))
            msg.AppendLine($"指令描述：{commandInfo.Desc}");
        msg.Append(errLog);

        Log.Error("CommandAdapter", msg.ToString());
        if (SendCommandErrMsg)
            switch (eventArgs.SourceType)
            {
                case SourceFlag.Group:
                    if (eventArgs is not GroupMessageEventArgs e)
                        break;
                    await ApiAdapter.SendGroupMessage(eventArgs.ConnId, e.SourceGroup, msg.ToString(), null)
                                    .RunCatch(er =>
                                    {
                                        Log.Error(er, "err cmd", "报错信息发送失败");
                                        return (new ApiStatus(), 0);
                                    });
                    break;
                case SourceFlag.Private:
                    await ApiAdapter.SendPrivateMessage(eventArgs.ConnId, eventArgs.Sender, msg.ToString(), null, null)
                                    .RunCatch(er =>
                                    {
                                        Log.Error(er, "err cmd", "报错信息发送失败");
                                        return (new ApiStatus(), 0);
                                    });
                    break;
            }

        //异常处理
        if (CmdExceptionHandler is not null)
            CmdExceptionHandler.Invoke(err, eventArgs, msg.ToString());
        else if (ThrowCommandErr)
            throw err;
    }

    private int GetNewDynamicPriority()
    {
        if (_dynamicCommands.Count == 0)
            return 0;
        return _dynamicCommands.Max(c => c.Priority) + 1;
    }

#endregion
}