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

#region Prefix + Regex Matching Tests

    /// <see cref="CommandManager.HandleMessageEventAsync" />
    [Fact]
    public async Task CommandManager_RegexWithGroupPrefix_AnchoredPattern_Matches()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                [@"^dice\s*(\d*)$"],
            MatchType.Regex,
            prefix: "/");

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("/dice 6")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.True(executed);
    }

    /// <see cref="CommandManager.HandleMessageEventAsync" />
    [Fact]
    public async Task CommandManager_RegexWithGroupPrefix_NoAnchor_Matches()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                ["dice"],
            MatchType.Regex,
            prefix: "/");

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("/dice")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.True(executed);
    }

    /// <see cref="CommandManager.HandleMessageEventAsync" />
    [Fact]
    public async Task CommandManager_FullMatchWithGroupPrefix_StillWorks()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                ["ping"],
            prefix: "/");

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("/ping")
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.True(executed);
    }

    /// <see cref="CommandManager.HandleMessageEventAsync" />
    [Fact]
    public async Task CommandManager_RegexWithGroupPrefix_NoMatch_DoesNotExecute()
    {
        CommandManager manager  = new();
        bool           executed = false;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                executed = true;
                await ValueTask.CompletedTask;
            },
                [@"^dice\s*(\d*)$"],
            MatchType.Regex,
            prefix: "/");

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("dice 6") // Missing prefix
                    }
            };

        await manager.HandleMessageEventAsync(evt, CT);
        Assert.False(executed);
    }

#endregion

#region Unregister Dynamic Command Tests

    /// <see cref="CommandManager.UnregisterDynamicCommand" />
    [Fact]
    public async Task CommandManager_UnregisterDynamicCommand_NoLongerMatches()
    {
        CommandManager manager = new();
        int            count   = 0;

        Guid commandId = manager.RegisterDynamicCommand(
            async _ =>
            {
                count++;
                await ValueTask.CompletedTask;
            },
                ["removeme"]);

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 100L, SenderId = 200L,
                        Body       = new MessageBody("removeme")
                    }
            };

        // First invocation should work
        await manager.HandleMessageEventAsync(evt, CT);
        Assert.Equal(1, count);

        // Unregister
        bool removed = manager.UnregisterDynamicCommand(commandId);
        Assert.True(removed);

        // Second invocation should not match
        evt.IsContinueEventChain = true;
        await manager.HandleMessageEventAsync(evt, CT);
        Assert.Equal(1, count);
    }

    /// <see cref="CommandManager.UnregisterDynamicCommand" />
    [Fact]
    public void CommandManager_UnregisterNonExistentId_ReturnsFalse()
    {
        CommandManager manager = new();
        bool           result  = manager.UnregisterDynamicCommand(Guid.NewGuid());
        Assert.False(result);
    }

#endregion

#region Regex Timeout Tests

    /// <see cref="RegexMatcher.IsMatch" />
    [Fact]
    public void RegexMatcher_TimeoutPattern_ReturnsFalse()
    {
        RegexMatcher matcher = new();
        // Catastrophic backtracking pattern with long input — should timeout
        string pattern = @"^(a+)+$";
        string input   = new string('a', 50) + "!";

        // This should return false (timeout) rather than throwing
        bool result = matcher.IsMatch(input, pattern);
        Assert.False(result);
    }

#endregion
}