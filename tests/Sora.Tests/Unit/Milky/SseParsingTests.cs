using System.Text;
using Sora.Adapter.Milky.Net;
using Xunit;

namespace Sora.Tests.Unit.Milky;

/// <summary>Tests for <see cref="MilkySseEventClient" /> SSE stream parsing.</summary>
[Collection("Milky.Unit")]
[Trait("Category", "Unit")]
public class SseParsingTests
{
#region Basic Parsing Tests

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task SingleDataLine_DispatchesMessage()
    {
        string       sse      = "data: {\"type\":\"test\"}\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("{\"type\":\"test\"}", messages[0]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task MultipleEvents_AllDispatched()
    {
        StringBuilder sb = new();
        sb.AppendLine("data: first");
        sb.AppendLine();
        sb.AppendLine("data: second");
        sb.AppendLine();
        sb.AppendLine("data: third");
        sb.AppendLine();

        List<string> messages = await ParseAsync(sb.ToString());

        Assert.Equal(3, messages.Count);
        Assert.Equal("first", messages[0]);
        Assert.Equal("second", messages[1]);
        Assert.Equal("third", messages[2]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task MultiLineData_JoinedWithNewline()
    {
        string       sse      = "data: hello\ndata: world\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("hello\nworld", messages[0]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task ThreeDataLines_JoinedWithNewlines()
    {
        string       sse      = "data: line1\ndata: line2\ndata: line3\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("line1\nline2\nline3", messages[0]);
    }

#endregion

#region Data Format Tests

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task DataWithoutSpace_DispatchesMessage()
    {
        string       sse      = "data:{\"type\":\"test\"}\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("{\"type\":\"test\"}", messages[0]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task DataWithExtraSpaces_PreservesAfterFirst()
    {
        // Per SSE spec, only the first space after colon is stripped
        string       sse      = "data:  two spaces\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal(" two spaces", messages[0]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task DataContainingColons_PreservesColons()
    {
        // JSON with colons in values: only the first colon splits field name
        string       sse      = "data: {\"url\":\"http://example.com:8080\"}\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("{\"url\":\"http://example.com:8080\"}", messages[0]);
    }

#endregion

#region Event Type Tests

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task NoEventType_Dispatched()
    {
        string       sse      = "data: payload\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task MilkyEventType_Dispatched()
    {
        string       sse      = "event: milky_event\ndata: {\"ok\":true}\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("{\"ok\":true}", messages[0]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task EventTypeWithoutSpace_Dispatched()
    {
        string       sse      = "event:milky_event\ndata: ok\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("ok", messages[0]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task UnknownEventType_NotDispatched()
    {
        string       sse      = "event: heartbeat\ndata: {}\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Empty(messages);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task MixedEventTypes_OnlyMilkyDispatched()
    {
        StringBuilder sb = new();
        sb.AppendLine("event: milky_event");
        sb.AppendLine("data: wanted");
        sb.AppendLine();
        sb.AppendLine("event: heartbeat");
        sb.AppendLine("data: unwanted");
        sb.AppendLine();
        sb.AppendLine("data: also-wanted");
        sb.AppendLine();

        List<string> messages = await ParseAsync(sb.ToString());

        Assert.Equal(2, messages.Count);
        Assert.Equal("wanted", messages[0]);
        Assert.Equal("also-wanted", messages[1]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task EventTypeResets_BetweenEvents()
    {
        StringBuilder sb = new();
        sb.AppendLine("event: heartbeat");
        sb.AppendLine("data: skip");
        sb.AppendLine();
        // Next event has no event type — should dispatch
        sb.AppendLine("data: keep");
        sb.AppendLine();

        List<string> messages = await ParseAsync(sb.ToString());

        Assert.Single(messages);
        Assert.Equal("keep", messages[0]);
    }

#endregion

#region Ignored Content Tests

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task CommentLines_Ignored()
    {
        string       sse      = ": this is a comment\ndata: real\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("real", messages[0]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task CommentBetweenDataLines_Ignored()
    {
        string       sse      = "data: first\n: comment\ndata: second\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("first\nsecond", messages[0]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task UnknownFields_Skipped()
    {
        string       sse      = "id: 42\nretry: 1000\ndata: payload\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("payload", messages[0]);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task LineWithNoColon_Skipped()
    {
        string       sse      = "invalid line\ndata: valid\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Single(messages);
        Assert.Equal("valid", messages[0]);
    }

#endregion

#region Edge Case Tests

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task EmptyDataField_NoDispatch()
    {
        // data: with empty value → buffer = "\n" → after stripping trailing LF → empty → no dispatch
        string       sse      = "data:\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Empty(messages);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task EmptyLineWithoutData_NoDispatch()
    {
        string       sse      = "\n\n\n";
        List<string> messages = await ParseAsync(sse);

        Assert.Empty(messages);
    }

    /// <see cref="MilkySseEventClient.ParseSseStreamAsync" />
    [Fact]
    public async Task CancelledToken_StopsProcessing()
    {
        CancellationTokenSource cts      = new();
        List<string>            messages = [];

        // Build a large stream
        StringBuilder sb = new();
        for (int i = 0; i < 100; i++)
        {
            sb.AppendLine($"data: msg{i}");
            sb.AppendLine();
        }

        // Cancel after first message
        using StringReader reader = new(sb.ToString());
        await cts.CancelAsync();

        await MilkySseEventClient.ParseSseStreamAsync(reader, msg => messages.Add(msg), cts.Token);

        Assert.Empty(messages);
    }

#endregion

    /// <summary>Helper that builds an SSE stream from raw text and parses it.</summary>
    private static async Task<List<string>> ParseAsync(string sseText)
    {
        List<string>       messages = [];
        using StringReader reader   = new(sseText);
        await MilkySseEventClient.ParseSseStreamAsync(reader, msg => messages.Add(msg), CancellationToken.None);
        return messages;
    }
}