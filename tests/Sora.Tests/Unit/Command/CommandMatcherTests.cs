using Xunit;

namespace Sora.Tests.Unit.Command;

/// <summary>Tests for <see cref="CommandManager" /> and <see cref="ICommandMatcher" /> implementations.</summary>
[Collection("Command.Unit")]
[Trait("Category", "Unit")]
public class CommandMatcherTests
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

#region Individual Matcher Tests

    /// <see cref="FullMatcher.IsMatch" />
    [Fact]
    public void FullMatcher_ExactMatch()
    {
        FullMatcher matcher = new();
        Assert.True(matcher.IsMatch("hello", "hello"));
        Assert.False(matcher.IsMatch("hello world", "hello"));
        Assert.False(matcher.IsMatch("Hello", "hello")); // Case sensitive
    }

    /// <see cref="ICommandMatcher.MatchType" />
    [Fact]
    public void FullMatcher_MatchType()
    {
        Assert.Equal(MatchType.Full, new FullMatcher().MatchType);
        Assert.Equal(MatchType.Regex, new RegexMatcher().MatchType);
        Assert.Equal(MatchType.Keyword, new KeywordMatcher().MatchType);
    }

    /// <see cref="RegexMatcher.IsMatch" />
    [Fact]
    public void RegexMatcher_PatternMatch()
    {
        RegexMatcher matcher = new();
        Assert.True(matcher.IsMatch("ban user123", @"^ban \w+$"));
        Assert.False(matcher.IsMatch("hello", @"^ban \w+$"));
    }

    /// <see cref="KeywordMatcher.IsMatch" />
    [Fact]
    public void KeywordMatcher_ContainsMatch()
    {
        KeywordMatcher matcher = new();
        Assert.True(matcher.IsMatch("say hello world", "hello"));
        Assert.False(matcher.IsMatch("say hi world", "hello"));
    }

#endregion

#region CommandManager Execution Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task CommandManager_DynamicCommand_Executes()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                ["ping"]);

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("ping")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.True(executed);
    }

    /// <see cref="CommandManager.HandleMessageEventAsync" />
    [Fact]
    public async Task CommandManager_NoMatch_DoesNotExecute()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                ["hello"]);

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("world")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.False(executed);
    }

    /// <see cref="CommandManager.HandleMessageEventAsync" />
    [Fact]
    public async Task CommandManager_EmptyMessage_NoExecution()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                [""]);

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody()
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.False(executed);
    }

#endregion

#region Match Type Tests

    /// <see cref="KeywordMatcher" />
    [Fact]
    public async Task CommandManager_KeywordMatch()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                ["help"],
            MatchType.Keyword);

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("I need help please")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.True(executed);
    }

    /// <see cref="RegexMatcher" />
    [Fact]
    public async Task CommandManager_RegexMatch()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                [@"^/ban \d+$"],
            MatchType.Regex);

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("/ban 12345")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.True(executed);
    }

#endregion

#region Priority and Blocking Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task CommandManager_Priority_HigherFirst()
    {
        CommandManager manager = new();
        List<string>   order   = [];

        manager.RegisterDynamicCommand(
            async _ =>
            {
                order.Add("low");
                await ValueTask.CompletedTask;
            },
                ["test"],
            priority: 1,
            blockAfterMatch: false);

        manager.RegisterDynamicCommand(
            async _ =>
            {
                order.Add("high");
                await ValueTask.CompletedTask;
            },
                ["test"],
            priority: 10,
            blockAfterMatch: false);

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("test")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.Equal(["high", "low"], order);
    }

    /// <see cref="CommandManager.HandleMessageEventAsync" />
    [Fact]
    public async Task CommandManager_BlockAfterMatch_StopsChain()
    {
        CommandManager manager = new();
        int            count   = 0;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                count++;
                await ValueTask.CompletedTask;
            },
                ["test"],
            priority: 10,
            blockAfterMatch: true);

        manager.RegisterDynamicCommand(
            async _ =>
            {
                count++;
                await ValueTask.CompletedTask;
            },
                ["test"],
            priority: 1,
            blockAfterMatch: true);

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("test")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.Equal(1, count);
    }

#endregion

#region Source Type Filter Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task CommandManager_SourceTypeFilter()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                ["test"],
            MatchType.Full,
            MessageSourceType.Friend);

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("test")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.False(executed);
    }

#endregion
}