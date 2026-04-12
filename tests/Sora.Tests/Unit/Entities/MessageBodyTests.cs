using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for <see cref="MessageBody" />.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class MessageBodyTests
{
#region Segment Properties Tests

    /// <see cref="Segment.Type" />
    [Fact]
    public void Segment_Types_Correct()
    {
        Assert.Equal(SegmentType.Text, new TextSegment { Text = "" }.Type);
        Assert.Equal(SegmentType.Image, new ImageSegment().Type);
        Assert.Equal(SegmentType.Mention, new MentionSegment { Target = default }.Type);
        Assert.Equal(SegmentType.MentionAll, new MentionAllSegment().Type);
        Assert.Equal(SegmentType.Reply, new ReplySegment { TargetId = default }.Type);
        Assert.Equal(SegmentType.Face, new FaceSegment { FaceId     = "0" }.Type);
        Assert.Equal(SegmentType.Audio, new AudioSegment().Type);
        Assert.Equal(SegmentType.Video, new VideoSegment().Type);
        Assert.Equal(SegmentType.Forward, new ForwardSegment().Type);
    }

    /// <see cref="Segment.Direction" />
    [Fact]
    public void Segment_Direction_Correct()
    {
        Assert.Equal(SegmentDirection.Both, new TextSegment { Text = "" }.Direction);
        Assert.Equal(SegmentDirection.Both, new ImageSegment().Direction);
        Assert.Equal(SegmentDirection.Incoming, new FileSegment().Direction);
        Assert.Equal(
            SegmentDirection.Incoming,
            new MarketFaceSegment { EmojiPackageId = 0, EmojiId = "", Key = "", Summary = "", Url = "" }.Direction);
        Assert.Equal(SegmentDirection.Incoming, new XmlSegment { ServiceId = 0, XmlPayload = "" }.Direction);
        Assert.Equal(SegmentDirection.Both, new LightAppSegment().Direction);
        Assert.Equal(SegmentDirection.Both, new ForwardSegment().Direction);
    }

    /// <see cref="Segment" />
    [Fact]
    public void Segment_Record_Equality()
    {
        TextSegment a = new() { Text = "hello" };
        TextSegment b = new() { Text = "hello" };
        Assert.Equal(a, b);

        MentionSegment c = new() { Target = 100L };
        MentionSegment d = new() { Target = 100L };
        Assert.Equal(c, d);
    }

    /// <see cref="ImageSegment.ResourceId" />
    [Fact]
    public void ImageSegment_ResourceId()
    {
        ImageSegment img = new() { ResourceId = "res123", Url = "http://temp", Width = 100 };
        Assert.Equal("res123", img.ResourceId);
    }

    /// <see cref="ForwardSegment" />
    [Fact]
    public void ForwardSegment_WithProperties()
    {
        ForwardSegment seg = new()
                { ForwardId = "fwd1", Title = "Title", Summary = "Summary", Preview = ["line1"] };
        Assert.Equal("fwd1", seg.ForwardId);
        Assert.Equal("Title", seg.Title);
        Assert.Equal("Summary", seg.Summary);
        Assert.Single(seg.Preview);
    }

#endregion

#region MessageBody Construction Tests

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_Empty()
    {
        MessageBody body = new();
        Assert.Empty(body);
        Assert.Equal("", body.GetText());
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_FromString()
    {
        MessageBody body = "hello";
        Assert.Single(body);
        Assert.IsType<TextSegment>(body[0]);
        Assert.Equal("hello", body.GetText());
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_ImplicitFromString()
    {
        MessageBody body = "implicit test";
        Assert.Equal("implicit test", body.GetText());
    }

    /// <see cref="MessageBody.FromIncoming" />
    [Fact]
    public void MessageBody_AllSegmentTypes()
    {
        // Use FromIncoming since this tests a received message with all segment types
        MessageBody body = MessageBody.FromIncoming(
            [
                new TextSegment { Text      = "text" },
                new ImageSegment { Url      = "http://img.png" },
                new MentionSegment { Target = 123L },
                new MentionAllSegment(),
                new ReplySegment { TargetId    = 456L },
                new FaceSegment { FaceId       = "1", IsLarge = true },
                new AudioSegment { Url         = "http://audio.mp3" },
                new VideoSegment { Url         = "http://video.mp4" },
                new FileSegment { FileId       = "fid", FileName = "test.txt", FileSize = 1024 },
                new ForwardSegment { ForwardId = "fwd123" },
                new MarketFaceSegment
                        { EmojiPackageId = 1, EmojiId = "eid", Key = "key", Summary = "summary", Url = "http://face.png" },
                new LightAppSegment { AppName = "app", JsonPayload = "{}" },
                new XmlSegment { ServiceId    = 1, XmlPayload      = "<xml/>" }
            ]);

        Assert.Equal(13, body.Count);
        Assert.Equal("text", body.GetText());

        List<ImageSegment> images = body.GetAll<ImageSegment>().ToList();
        Assert.Single(images);
        Assert.Equal("http://img.png", images[0].Url);
    }

#endregion

#region MessageBody IList Operations Tests

    /// <see cref="MessageBody.Add" />
    [Fact]
    public void Add_IncomingOnlySegment_SilentlyDropped()
    {
        MessageBody body = new();
        body.Add(
            new MarketFaceSegment
                {
                    EmojiPackageId = 1, EmojiId = "e1", Key = "k1", Summary = "s", Url = "http://url"
                });
        Assert.Empty(body);
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_IList_Operations()
    {
        TextSegment seg1 = new() { Text = "hello" };
        TextSegment seg2 = new() { Text = "world" };
        MessageBody body = new([seg1, seg2]);
        Assert.Equal(2, body.Count);
        Assert.Contains(seg1, body);

        body.RemoveAt(0);
        Assert.Single(body);
        Assert.Equal("world", body.GetText());
    }

    /// <see cref="MessageBody.Insert" />
    [Fact]
    public void MessageBody_Insert()
    {
        MessageBody body = new("world");
        body.Insert(0, new TextSegment { Text = "hello " });
        Assert.Equal("hello world", body.GetText());
    }

    /// <see cref="MessageBody.Remove" />
    [Fact]
    public void MessageBody_Remove_ByItem()
    {
        TextSegment seg     = new() { Text = "hello" };
        MessageBody body    = new([seg, new TextSegment { Text = "world" }]);
        bool        removed = body.Remove(seg);
        Assert.True(removed);
        Assert.Single(body);
    }

    /// <see cref="MessageBody.Remove" />
    [Fact]
    public void MessageBody_Remove_NonExistent_ReturnsFalse()
    {
        MessageBody body    = new("test");
        bool        removed = body.Remove(new TextSegment { Text = "nothere" });
        Assert.False(removed);
    }

    /// <see cref="MessageBody.this[int]" />
    [Fact]
    public void MessageBody_SetByIndex()
    {
        MessageBody body = new([new TextSegment { Text = "old" }]);
        body[0] = new TextSegment { Text = "new" };
        Assert.Equal("new", body.GetText());
    }

    /// <see cref="MessageBody.IndexOf" />
    [Fact]
    public void MessageBody_IndexOf()
    {
        TextSegment seg  = new() { Text = "target" };
        MessageBody body = new([new TextSegment { Text = "a" }, seg, new TextSegment { Text = "b" }]);
        Assert.Equal(1, body.IndexOf(seg));
    }

    /// <see cref="MessageBody.CopyTo" />
    [Fact]
    public void MessageBody_CopyTo()
    {
        MessageBody body = new([new TextSegment { Text = "a" }, new TextSegment { Text = "b" }]);
        Segment[]   arr  = new Segment[2];
        body.CopyTo(arr, 0);
        Assert.IsType<TextSegment>(arr[0]);
        Assert.IsType<TextSegment>(arr[1]);
    }

    /// <see cref="MessageBody.Clear" />
    [Fact]
    public void MessageBody_Clear()
    {
        MessageBody body = new("test");
        Assert.NotEmpty(body);
        body.Clear();
        Assert.Empty(body);
    }

    /// <see cref="MessageBody.GetEnumerator" />
    [Fact]
    public void MessageBody_Foreach_Enumeration()
    {
        MessageBody   body  = new([new TextSegment { Text = "a" }, new TextSegment { Text = "b" }]);
        List<Segment> items = [..body];
        Assert.Equal(2, items.Count);
    }

#endregion

#region MessageBody Query Tests

    /// <see cref="MessageBody.GetText" />
    [Fact]
    public void MessageBody_GetText_ConcatenatesTextSegments()
    {
        MessageBody body = new(
            [
                new TextSegment { Text = "hello " }, new MentionSegment { Target = 123L }, new TextSegment { Text = "world" }
            ]);
        Assert.Equal("hello world", body.GetText());
    }

    /// <see cref="MessageBody.GetFirst{T}" />
    [Fact]
    public void MessageBody_GetFirst()
    {
        MessageBody body = new(
            [
                new TextSegment { Text = "text" }, new ImageSegment { Url = "http://img.png" }, new TextSegment { Text = "more" }
            ]);

        ImageSegment? img = body.GetFirst<ImageSegment>();
        Assert.NotNull(img);
        Assert.Equal("http://img.png", img.Url);
    }

    /// <see cref="MessageBody.GetAll{T}" />
    [Fact]
    public void MessageBody_GetAll()
    {
        MessageBody body = new([new TextSegment { Text = "a" }, new ImageSegment(), new TextSegment { Text = "b" }]);

        List<TextSegment> texts = body.GetAll<TextSegment>().ToList();
        Assert.Equal(2, texts.Count);
    }

#endregion

#region Fluent Builder Tests

    /// <see cref="MessageBody.AddText" />
    [Fact]
    public void FluentBuilder_Chain()
    {
        MessageBody body = new MessageBody()
                           .AddText("hello ")
                           .AddMention(123L)
                           .AddText(" world");
        Assert.Equal(3, body.Count);
        Assert.Equal("hello  world", body.GetText());
    }

    /// <see cref="MessageBody.AddImage" />
    [Fact]
    public void FluentBuilder_Image()
    {
        MessageBody body = new MessageBody().AddImage("file://test.png", ImageSubType.Sticker);
        Assert.Single(body);
        ImageSegment img = (ImageSegment)body[0];
        Assert.Equal("file://test.png", img.FileUri);
        Assert.Equal(ImageSubType.Sticker, img.SubType);
    }

    /// <see cref="SegmentBuilder" />
    [Fact]
    public void SegmentBuilder_AllMethods()
    {
        Assert.IsType<TextSegment>(SegmentBuilder.Text("hi"));
        Assert.IsType<MentionSegment>(SegmentBuilder.Mention(1L));
        Assert.IsType<MentionAllSegment>(SegmentBuilder.MentionAll());
        Assert.IsType<FaceSegment>(SegmentBuilder.Face("1"));
        Assert.IsType<ReplySegment>(SegmentBuilder.Reply(1L));
        Assert.IsType<ImageSegment>(SegmentBuilder.Image("file://x"));
        Assert.IsType<AudioSegment>(SegmentBuilder.Audio("file://x"));
        Assert.IsType<VideoSegment>(SegmentBuilder.Video("file://x"));
        Assert.IsType<LightAppSegment>(SegmentBuilder.LightApp("app", "{}"));
    }

#endregion

#region ToOutgoing Tests

    /// <see cref="ImageSegment.ToOutgoing" />
    [Fact]
    public void ToOutgoing_ImageUsesUrlFallback()
    {
        ImageSegment incoming = new() { Url = "http://temp.url/img.png", Width = 100 };
        Segment?     outgoing = incoming.ToOutgoing();
        Assert.NotNull(outgoing);
        ImageSegment outImg = (ImageSegment)outgoing;
        Assert.Equal("http://temp.url/img.png", outImg.FileUri);
    }

    /// <see cref="ImageSegment.ToOutgoing" />
    [Fact]
    public void ToOutgoing_ImageWithFileUri_KeepsIt()
    {
        ImageSegment img      = new() { FileUri = "base64://abc", Url = "http://old" };
        Segment?     outgoing = img.ToOutgoing();
        Assert.NotNull(outgoing);
        Assert.Equal("base64://abc", ((ImageSegment)outgoing).FileUri);
    }

    /// <see cref="MessageBody.ToOutgoing" />
    [Fact]
    public void ToOutgoing_SkipsIncomingOnly()
    {
        // Use FromIncoming to simulate a received message with mixed segments
        MessageBody body = MessageBody.FromIncoming(
            [
                new TextSegment { Text                 = "hello" },
                new MarketFaceSegment { EmojiPackageId = 1, EmojiId    = "e1", Key = "k1", Summary = "s", Url = "http://url" },
                new XmlSegment { ServiceId             = 1, XmlPayload = "<xml/>" },
                new MentionSegment { Target            = 123L }
            ]);

        MessageBody outgoing = body.ToOutgoing();
        Assert.Equal(2, outgoing.Count);
        Assert.IsType<TextSegment>(outgoing[0]);
        Assert.IsType<MentionSegment>(outgoing[1]);
    }

    /// <see cref="ForwardSegment.ToOutgoing" />
    [Fact]
    public void ToOutgoing_ForwardWithMessages_ReturnsThis()
    {
        ForwardSegment fwd = new()
            {
                Messages =
                    [
                        new ForwardedMessageNode
                                { UserId = 1L, SenderName = "A", Segments = [new TextSegment { Text = "hi" }] }
                    ]
            };
        Assert.NotNull(fwd.ToOutgoing());
    }

    /// <see cref="ForwardSegment.ToOutgoing" />
    [Fact]
    public void ToOutgoing_ForwardWithoutMessages_ReturnsNull()
    {
        ForwardSegment fwd = new() { ForwardId = "fwd123" };
        Assert.Null(fwd.ToOutgoing());
    }

#endregion

#region Validate Tests

    /// <see cref="MessageBody.IsValidForSending" />
    [Fact]
    public void Validate_ValidMessage_NoIssues()
    {
        MessageBody body = new([new TextSegment { Text = "hello" }, new MentionSegment { Target = 123L }]);
        Assert.True(body.IsValidForSending());
    }

    /// <see cref="MessageBody.IsValidForSending" />
    [Fact]
    public void Validate_SoloAudio_Valid()
    {
        MessageBody body = new([new AudioSegment { FileUri = "file://test.mp3" }]);
        Assert.True(body.IsValidForSending());
    }

    /// <see cref="MessageBody.Validate" />
    [Fact]
    public void Validate_AudioMustBeSolo()
    {
        MessageBody           body   = new([new TextSegment { Text = "hello" }, new AudioSegment { FileUri = "file://test.mp3" }]);
        IReadOnlyList<string> issues = body.Validate();
        Assert.NotEmpty(issues);
        Assert.Contains(issues, i => i.Contains("Audio"));
    }

    /// <see cref="MessageBody.Validate" />
    [Fact]
    public void Validate_VideoMustBeSolo()
    {
        MessageBody           body   = new([new TextSegment { Text = "hello" }, new VideoSegment { FileUri = "file://test.mp4" }]);
        IReadOnlyList<string> issues = body.Validate();
        Assert.NotEmpty(issues);
    }

    /// <see cref="MessageBody.Validate" />
    [Fact]
    public void Validate_ForwardMustBeSolo()
    {
        MessageBody body = new(
            [
                new TextSegment { Text = "hello" },
                new ForwardSegment
                    {
                        Messages =
                            [
                                new ForwardedMessageNode
                                        { UserId = 1L, SenderName = "A", Segments = [new TextSegment { Text = "hi" }] }
                            ]
                    }
            ]);
        IReadOnlyList<string> issues = body.Validate();
        Assert.NotEmpty(issues);
    }

    /// <see cref="MessageBody.Validate" />
    [Fact]
    public void Validate_LargeFaceMustBeSolo()
    {
        MessageBody           body   = new([new TextSegment { Text = "hello" }, new FaceSegment { FaceId = "1", IsLarge = true }]);
        IReadOnlyList<string> issues = body.Validate();
        Assert.NotEmpty(issues);
    }

#endregion
}