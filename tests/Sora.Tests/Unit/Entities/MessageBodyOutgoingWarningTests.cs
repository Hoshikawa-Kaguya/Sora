using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests the warnings logged by <see cref="MessageBody.ToOutgoing" /> for dropped segments.</summary>
[Collection("Entities.StaticState")]
[Trait("Category", "Unit")]
public class MessageBodyOutgoingWarningTests
{
    /// <see cref="MessageBody.ToOutgoing" />
    [Fact]
    public void ToOutgoing_LogsWarningForEachDroppedSegment()
    {
        MessageBody body =
        [
            new TextSegment { Text = "hi" },
            new FileSegment { FileId = "fid" },
            new ImageSegment(),
            new MarkdownSegment { Content = "**md**" }
        ];

        List<string> warnings = CaptureWarnings(() => Assert.Single(body.ToOutgoing()));

        Assert.Equal([Dropped(SegmentType.File, 1), Dropped(SegmentType.Image, 2), Dropped(SegmentType.Markdown, 3)], warnings);
    }

    /// <see cref="MessageBody.ToOutgoing" />
    [Fact]
    public void ToOutgoing_LogsWarningForSegmentDroppedInNestedForward()
    {
        MessageBody body =
        [
            new ForwardSegment
            {
                Messages =
                [
                    new ForwardedMessageNode
                        { UserId = 1L, Segments = [new TextSegment { Text = "inner" }, new FileSegment { FileId = "fid" }] }
                ]
            }
        ];

        List<string> warnings = CaptureWarnings(() => Assert.Single(body.ToOutgoing()));

        Assert.Equal(Dropped(SegmentType.File, 1), Assert.Single(warnings));
    }

    /// <see cref="MessageBody.ToOutgoing" />
    [Fact]
    public void ToOutgoing_SendableBody_LogsNothing()
    {
        MessageBody body = new MessageBody().AddText("hi").AddImage("base64://abc");

        List<string> warnings = CaptureWarnings(() => Assert.Equal(2, body.ToOutgoing().Count));

        Assert.Empty(warnings);
    }

    private static string Dropped(SegmentType type, int index) =>
        $"Segment {type} at index {index} cannot be sent and was dropped";

    private static List<string> CaptureWarnings(Action action)
    {
        ILogger              original = MessageBody.Logger;
        WarningCaptureLogger capture  = new();
        MessageBody.Logger = capture;
        try
        {
            action();
        }
        finally
        {
            MessageBody.Logger = original;
        }

        return capture.Warnings;
    }
}
