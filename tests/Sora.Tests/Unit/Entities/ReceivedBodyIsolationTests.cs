using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>
///     Tests that received message content (<see cref="MessageContext.Body" />, <see cref="ReplySegment.Content" />,
///     <see cref="GroupEssenceMessageInfo.Body" /> and <see cref="ForwardedMessageNode.Segments" />) is read as
///     independent editable copies, so no handler entry can change what later entries see.
/// </summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class ReceivedBodyIsolationTests : IAsyncDisposable
{
    private const string ImageUrl = "http://img.png";

    private readonly EventPipelineFilterTests.MockAdapter _adapter = new();
    private readonly SoraService                         _service;

    public ReceivedBodyIsolationTests()
    {
        _service = new SoraService(_adapter, new ServiceConfig());
    }

    public async ValueTask DisposeAsync() => await _service.DisposeAsync();

#region MessageContext

    /// <see cref="MessageContext.Body" />
    [Fact]
    public void MessageContextBody_EditsToCopyDoNotAffectContext()
    {
        MessageContext context = new() { Body = Received() };

        MessageBody copy = context.Body;
        copy.Add(new TextSegment { Text = "!" });
        ((ImageSegment)copy[1]).FileUri = "base64://edited";
        copy.RemoveAt(0);
        _ = context.Body + "appended";
        context.Body.Clear();

        AssertReceived(context.Body);
    }

    /// <see cref="MessageContext.Body" />
    [Fact]
    public void MessageContextBody_ReadsAreEqualButIndependent()
    {
        MessageContext context = new() { Body = Received() };

        MessageBody first  = context.Body;
        MessageBody second = context.Body;

        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        Assert.NotSame(first[1], second[1]);
        Assert.Equal(context, context with { });
    }

    /// <see cref="MessageContext.Body" />
    [Fact]
    public void MessageContextBody_KeepsIncomingOnlySegments()
    {
        FileSegment    file    = new() { FileId = "fid" };
        MessageContext context = new() { Body = MessageBody.FromIncoming([new TextSegment { Text = "hi" }, file]) };

        FileSegment copied = Assert.IsType<FileSegment>(context.Body[1]);
        Assert.Equal("fid", copied.FileId);
        Assert.NotSame(file, copied);
    }

    /// <see cref="MessageContext.Body" />
    [Fact]
    public void MessageContextBody_LaterChangesToInitializerAreNotObserved()
    {
        MessageBody    source  = Received();
        MessageContext context = new() { Body = source };

        source.Clear();

        AssertReceived(context.Body);
    }

#endregion

#region Nested Content

    /// <see cref="ReplySegment.Content" />
    [Fact]
    public void ReplyContent_EditsToCopyDoNotAffectSegment()
    {
        ReplySegment   reply   = new() { TargetId = 1L, Content = Received() };
        MessageContext context = new() { Body = MessageBody.FromIncoming([reply]) };

        ReplySegment quoted  = Assert.IsType<ReplySegment>(context.Body[0]);
        MessageBody  content = Assert.IsType<MessageBody>(quoted.Content);
        content.Clear();
        ((ImageSegment)Assert.IsType<MessageBody>(quoted.Content)[1]).FileUri = "base64://edited";

        AssertReceived(Assert.IsType<ReplySegment>(context.Body[0]).Content);
        Assert.Equal(context.Body, context.Body);
    }

    /// <see cref="GroupEssenceMessageInfo.Body" />
    [Fact]
    public void GroupEssenceBody_EditsToCopyDoNotAffectInfo()
    {
        GroupEssenceMessageInfo info = new() { Body = Received() };

        info.Body.Clear();
        ((ImageSegment)info.Body[1]).FileUri = "base64://edited";

        AssertReceived(info.Body);
    }

    /// <see cref="ForwardedMessageNode.Segments" />
    [Fact]
    public void ForwardedNodeSegments_EditsToCopyAreNotSavedToNode()
    {
        ForwardedMessageNode node = new() { UserId = 1L, Segments = Received() };

        node.Segments.Add(new TextSegment { Text = "!" });
        ((ImageSegment)node.Segments[1]).FileUri = "base64://edited";

        AssertReceived(node.Segments);
    }

    /// <see cref="ForwardedMessageNode.Segments" />
    [Fact]
    public void ForwardedNodeSegments_NestedThreeLevelsAreIsolated()
    {
        ForwardSegment level3 = Forward(Received());
        ForwardSegment level2 = Forward(new MessageBody([level3]));
        ForwardSegment level1 = Forward(new MessageBody([level2]));
        MessageContext context = new() { Body = new MessageBody([level1]) };

        MessageBody innermost = Innermost(context.Body);
        innermost.Clear();
        ((ImageSegment)Innermost(context.Body)[1]).FileUri = "base64://edited";

        AssertReceived(Innermost(context.Body));
        Assert.Equal(context.Body, context.Body);
    }

#endregion

#region Handler Entries

    /// <summary>
    ///     A command that edits the event body does not affect the matching or execution of a later command, nor an
    ///     event subscriber.
    /// </summary>
    [Fact]
    public async Task HandlerEntries_EditsByEarlierEntryAreNotSeenByLaterEntries()
    {
        List<MessageBody> seen = [];
        _service.Commands.TestRegistrar().RegisterDynamicCommand(
            typeof(ReceivedBodyIsolationTests),
            e =>
            {
                MessageBody body = e.Message.Body;
                body.Clear();
                body.Add(new TextSegment { Text = "changed" });
                _ = e.Message.Body + "!";
                ((ImageSegment)e.Message.Body[1]).FileUri = "base64://edited";
                return ValueTask.CompletedTask;
            },
            ["hello"],
            priority: 10,
            blockAfterMatch: false);
        _service.Commands.TestRegistrar().RegisterDynamicCommand(
            typeof(ReceivedBodyIsolationTests),
            e =>
            {
                seen.Add(e.Message.Body);
                return ValueTask.CompletedTask;
            },
            ["hello"],
            blockAfterMatch: false);
        _service.Events.OnMessageReceived += e =>
        {
            seen.Add(e.Message.Body);
            return ValueTask.CompletedTask;
        };

        await _adapter.RaiseEventAsync(
            new MessageReceivedEvent
            {
                Api          = null!,
                ConnectionId = Guid.NewGuid(),
                SelfId       = 1L,
                Time         = DateTime.Now,
                Message = new MessageContext
                {
                    MessageId = 1, SourceType = MessageSourceType.Friend, SenderId = 200L,
                    Body      = Received()
                }
            });

        Assert.Equal(2, seen.Count);
        Assert.All(seen, AssertReceived);
    }

#endregion

#region Helpers

    private static MessageBody Received() =>
        MessageBody.FromIncoming([new TextSegment { Text = "hello" }, new ImageSegment { Url = ImageUrl }]);

    private static void AssertReceived(MessageBody? body)
    {
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
        Assert.Equal("hello", body.GetText());
        ImageSegment image = Assert.IsType<ImageSegment>(body[1]);
        Assert.Equal(ImageUrl, image.Url);
        Assert.Equal("", image.FileUri);
        Assert.Equal(SegmentDirection.Both, image.Direction);
    }

    private static ForwardSegment Forward(MessageBody segments) =>
        new() { Messages = [new ForwardedMessageNode { UserId = 1L, Segments = segments }] };

    private static MessageBody Innermost(MessageBody body)
    {
        MessageBody current = body;
        for (int i = 0; i < 3; i++)
            current = Assert.Single(Assert.IsType<ForwardSegment>(current[0]).Messages).Segments;
        return current;
    }

    private sealed class ServiceConfig : IBotServiceConfig
    {
        public UserId[] SuperUsers           => [];
        public UserId[] BlockUsers           => [];
        public bool     AutoMarkMessageRead  => false;
        public bool     EnableCommandManager => true;
    }

#endregion
}
