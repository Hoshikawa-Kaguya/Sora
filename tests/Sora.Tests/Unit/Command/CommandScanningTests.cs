using Xunit;

namespace Sora.Tests.Unit.Command;

/// <summary>Test command group for scanning tests.</summary>
[CommandGroup(Name = "test", Prefix = "/")]
public static class TestGroupCommands
{
    /// <see cref="CommandAttribute.PermissionLevel" />
    [Command(Expressions = ["admin"], MatchType = MatchType.Full, PermissionLevel = MemberRole.Admin)]
    public static async ValueTask AdminOnly(MessageReceivedEvent e) => await ValueTask.CompletedTask;

    /// <see cref="CommandGroupAttribute" />
    [Command(Expressions = ["hello"], MatchType = MatchType.Full)]
    public static async ValueTask Hello(MessageReceivedEvent e) => await ValueTask.CompletedTask;
}

/// <summary>Test commands without a group for scanning tests.</summary>
public static class NoGroupCommands
{
    /// <see cref="CommandAttribute" />
    [Command(Expressions = ["standalone"], MatchType = MatchType.Full)]
    public static async ValueTask Standalone(MessageReceivedEvent e) => await ValueTask.CompletedTask;
}

/// <summary>Test commands with invalid signatures for scanning tests.</summary>
public static class BadSignatureCommands
{
    /// <see cref="CommandManager.ScanType(Type)" />
    [Command(Expressions = ["bad"])]
    public static void NonValueTask(MessageReceivedEvent e)
    {
    } // Wrong return type

    /// <see cref="CommandManager.ScanType(Type)" />
    [Command(Expressions = ["bad2"])]
    public static async ValueTask WrongParam(string s) => await ValueTask.CompletedTask; // Wrong param
}

/// <summary>Test instance command group for singleton verification.</summary>
[CommandGroup(Name = "instance-test", Prefix = "!")]
public class InstanceCommandGroup
{
    /// <summary>Tracks how many times this instance has been invoked.</summary>
    public int InvokeCount { get; set; }

    /// <see cref="CommandManager.ScanType(Type)" />
    [Command(Expressions = ["count"], MatchType = MatchType.Full)]
    public async ValueTask Count(MessageReceivedEvent e)
    {
        InvokeCount++;
        await ValueTask.CompletedTask;
    }

    /// <see cref="CommandManager.ScanType(Type)" />
    [Command(Expressions = ["greet"], MatchType = MatchType.Full)]
    public async ValueTask Greet(MessageReceivedEvent e)
    {
        InvokeCount++;
        await ValueTask.CompletedTask;
    }
}

/// <summary>Tests for <see cref="CommandManager" /> assembly scanning.</summary>
[Collection("Command.Unit")]
[Trait("Category", "Unit")]
public class CommandScanningTests
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

#region ScanType Tests

    /// <see cref="CommandManager.ScanType(Type)" />
    [Fact]
    public void ScanAssembly_FindsAttributedCommands()
    {
        CommandManager manager = new();
        manager.ScanType(typeof(TestGroupCommands));
        // The manager should find commands without throwing
    }

    /// <see cref="CommandManager.ScanType(Type)" />
    [Fact]
    public void ScanAssembly_SkipsBadSignatures()
    {
        CommandManager manager = new();
        // Should not throw even with bad signatures
        manager.ScanType(typeof(BadSignatureCommands));
    }

    /// <see cref="CommandGroupAttribute.Prefix" />
    [Fact]
    public async Task ScanAssembly_GroupPrefix_Applied()
    {
        CommandManager manager = new();
        manager.ScanType(typeof(TestGroupCommands));

        MessageReceivedEvent evt = CreateTestEvent("/hello");
        await manager.HandleMessageEventAsync(evt, CT);

        // The scanned command with prefix "/" and expression "hello" should match "/hello"
        // BlockAfterMatch defaults to true, so event chain should be stopped
        Assert.False(evt.IsContinueEventChain);
    }

    /// <see cref="CommandManager.ScanType(Type)" />
    [Fact]
    public async Task ScanAssembly_StandaloneCommand_NoPrefix()
    {
        CommandManager manager = new();
        manager.ScanType(typeof(NoGroupCommands));

        MessageReceivedEvent evt = CreateTestEvent("standalone");
        await manager.HandleMessageEventAsync(evt, CT);
        // Should match "standalone" with no prefix and block the chain
        Assert.False(evt.IsContinueEventChain);
    }

#endregion

#region Command Execution Tests

    /// <see cref="CommandManager.ScanType(Type)" />
    [Fact]
    public async Task ScanAssembly_StaticCommand_Executes()
    {
        CommandManager manager = new();
        manager.ScanType(typeof(TestGroupCommands));

        MessageReceivedEvent evt = CreateTestEvent("/hello");
        await manager.HandleMessageEventAsync(evt, CT);

        Assert.False(evt.IsContinueEventChain);
    }

    /// <see cref="CommandManager.ScanType(Type)" />
    [Fact]
    public async Task ScanAssembly_InstanceCommand_Executes()
    {
        CommandManager manager = new();
        manager.ScanType(typeof(InstanceCommandGroup));

        MessageReceivedEvent evt = CreateTestEvent("!count");
        await manager.HandleMessageEventAsync(evt, CT);

        Assert.False(evt.IsContinueEventChain);
    }

    /// <see cref="CommandManager.ScanType(Type)" />
    [Fact]
    public async Task ScanAssembly_InstanceCommand_UsesSingleton()
    {
        CommandManager manager = new();
        manager.ScanType(typeof(InstanceCommandGroup));

        // Execute "count" three times + "greet" once — all on the same singleton
        MessageReceivedEvent evt1 = CreateTestEvent("!count");
        await manager.HandleMessageEventAsync(evt1, CT);

        MessageReceivedEvent evt2 = CreateTestEvent("!count");
        await manager.HandleMessageEventAsync(evt2, CT);

        MessageReceivedEvent evt3 = CreateTestEvent("!count");
        await manager.HandleMessageEventAsync(evt3, CT);

        MessageReceivedEvent evt4 = CreateTestEvent("!greet");
        await manager.HandleMessageEventAsync(evt4, CT);

        // All 4 invocations share the same singleton instance
        InstanceCommandGroup? instance = manager.GetCommandInstance<InstanceCommandGroup>();
        Assert.NotNull(instance);
        Assert.Equal(4, instance.InvokeCount);
    }

#endregion

#region RegisterCommandInstance Tests

    /// <see cref="CommandManager.RegisterCommandInstance{T}" />
    [Fact]
    public async Task RegisterCommandInstance_UsesProvidedInstance()
    {
        CommandManager manager = new();

        // Pre-register an externally constructed instance
        InstanceCommandGroup external = new() { InvokeCount = 10 };
        manager.RegisterCommandInstance(external);

        manager.ScanType(typeof(InstanceCommandGroup));

        MessageReceivedEvent evt = CreateTestEvent("!count");
        await manager.HandleMessageEventAsync(evt, CT);

        // Should use the pre-registered instance, not create a new one
        InstanceCommandGroup? retrieved = manager.GetCommandInstance<InstanceCommandGroup>();
        Assert.NotNull(retrieved);
        Assert.Same(external, retrieved);
        Assert.Equal(11, retrieved.InvokeCount);
    }

    /// <see cref="CommandManager.RegisterCommandInstance{T}" />
    [Fact]
    public void RegisterCommandInstance_ThrowsAfterScan()
    {
        CommandManager manager = new();
        manager.ScanType(typeof(InstanceCommandGroup));

        InstanceCommandGroup external = new();
        Assert.Throws<InvalidOperationException>(() => manager.RegisterCommandInstance(external));
    }

#endregion

#region Permission Tests

    /// <see cref="CommandAttribute.PermissionLevel" />
    [Fact]
    public async Task ScanAssembly_PermissionLevel_AllowsHigherRole()
    {
        CommandManager manager = new();
        manager.ScanType(typeof(TestGroupCommands));

        MessageReceivedEvent evt = CreateTestEvent("/admin", MemberRole.Admin);
        await manager.HandleMessageEventAsync(evt, CT);
        // AdminOnly command should execute for Admin and block the chain
        Assert.False(evt.IsContinueEventChain);
    }

    /// <see cref="CommandAttribute.PermissionLevel" />
    [Fact]
    public async Task ScanAssembly_PermissionLevel_BlocksLowerRole()
    {
        CommandManager manager = new();
        manager.ScanType(typeof(TestGroupCommands));

        // Create event from a regular member trying "/admin" command
        MessageReceivedEvent evt = CreateTestEvent("/admin");
        await manager.HandleMessageEventAsync(evt, CT);
        // AdminOnly command requires Admin role, should NOT execute for Member
        Assert.True(evt.IsContinueEventChain);
    }

#endregion

    /// <summary>Creates a synthetic <see cref="MessageReceivedEvent" /> for command testing.</summary>
    private static MessageReceivedEvent CreateTestEvent(string text, MemberRole role = MemberRole.Member) =>
        new()
            {
                Api          = null!,
                ConnectionId = Guid.NewGuid(),
                SelfId       = 1L,
                Time         = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L,
                        SenderId   = 200L,
                        Body       = new MessageBody(text)
                    },
                Member = new GroupMemberInfo { UserId = 200L, GroupId = 100L, Role = role }
            };
}