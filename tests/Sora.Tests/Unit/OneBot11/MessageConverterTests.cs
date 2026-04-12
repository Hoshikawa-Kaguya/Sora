using Newtonsoft.Json.Linq;
using Sora.Adapter.OneBot11.Converter;
using Sora.Adapter.OneBot11.Models;
using Sora.Adapter.OneBot11.Segments;
using Xunit;

namespace Sora.Tests.Unit.OneBot11;

/// <summary>Tests for <see cref="MessageConverter" /> (OneBot11).</summary>
[Collection("OneBot11.Unit")]
[Trait("Category", "Unit")]
public class MessageConverterTests
{
#region Round-Trip Tests

    /// <summary>Verifies <see cref="MessageConverter" /> round-trips a text message.</summary>
    [Fact]
    public void RoundTrip_TextMessage()
    {
        MessageBody         original   = new("test message");
        List<OneBotSegment> obSegments = MessageConverter.ToOneBotSegments(original);
        JArray              json       = JArray.FromObject(obSegments);
        MessageBody         restored   = MessageConverter.ToMessageBody(json);
        Assert.Equal("test message", restored.GetText());
    }

    /// <summary>
    ///     Verifies <see cref="MessageConverter" /> round-trips a dice segment (result is incoming-only, lost on
    ///     round-trip).
    /// </summary>
    [Fact]
    public void RoundTrip_DiceSegment()
    {
        JArray incoming = new()
            {
                new JObject { ["type"] = "dice", ["data"] = new JObject { ["result"] = "5" } }
            };
        MessageBody         body    = MessageConverter.ToMessageBody(incoming);
        List<OneBotSegment> outSegs = MessageConverter.ToOneBotSegments(body);
        Assert.Single(outSegs);
        Assert.Equal("dice", outSegs[0].Type);
    }

    /// <summary>Verifies <see cref="MessageConverter" /> round-trips an RPS segment.</summary>
    [Fact]
    public void RoundTrip_RpsSegment()
    {
        JArray incoming = new()
            {
                new JObject { ["type"] = "rps", ["data"] = new JObject { ["result"] = "1" } }
            };
        MessageBody         body    = MessageConverter.ToMessageBody(incoming);
        List<OneBotSegment> outSegs = MessageConverter.ToOneBotSegments(body);
        Assert.Single(outSegs);
        Assert.Equal("rps", outSegs[0].Type);
    }

#endregion

#region ToMessageBody Tests

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a text segment.</summary>
    [Fact]
    public void ToMessageBody_TextSegment()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "text",
                        ["data"] = new JObject { ["text"] = "hello" }
                    }
            };

        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        Assert.IsType<TextSegment>(body[0]);
        Assert.Equal("hello", ((TextSegment)body[0]).Text);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts an at-user segment.</summary>
    [Fact]
    public void ToMessageBody_AtSegment_User()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "at",
                        ["data"] = new JObject { ["qq"] = "123456", ["name"] = "TestUser" }
                    }
            };

        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        Assert.IsType<MentionSegment>(body[0]);
        MentionSegment mention = (MentionSegment)body[0];
        Assert.Equal(123456L, (long)mention.Target);
        Assert.Equal("TestUser", mention.Name);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts an at-all segment.</summary>
    [Fact]
    public void ToMessageBody_AtAll()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "at",
                        ["data"] = new JObject { ["qq"] = "all" }
                    }
            };

        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        Assert.IsType<MentionAllSegment>(body[0]);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a face segment.</summary>
    [Fact]
    public void ToMessageBody_FaceSegment()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "face", ["data"] = new JObject { ["id"] = "178" } }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.IsType<FaceSegment>(body[0]);
        Assert.Equal("178", ((FaceSegment)body[0]).FaceId);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a reply segment.</summary>
    [Fact]
    public void ToMessageBody_ReplySegment()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "reply", ["data"] = new JObject { ["id"] = 42 } }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.IsType<ReplySegment>(body[0]);
        Assert.Equal(42L, (long)((ReplySegment)body[0]).TargetId);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts an image segment.</summary>
    [Fact]
    public void ToMessageBody_ImageSegment()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "image",
                        ["data"] = new JObject { ["url"] = "http://img.png", ["file"] = "abc.image" }
                    }
            };
        MessageBody  body = MessageConverter.ToMessageBody(json);
        ImageSegment img  = (ImageSegment)body[0];
        Assert.Equal("http://img.png", img.Url);
        Assert.Equal("abc.image", img.FileUri);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a record segment.</summary>
    [Fact]
    public void ToMessageBody_RecordSegment()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "record", ["data"] = new JObject { ["url"] = "http://audio.amr" } }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.IsType<AudioSegment>(body[0]);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a video segment.</summary>
    [Fact]
    public void ToMessageBody_VideoSegment()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "video", ["data"] = new JObject { ["url"] = "http://video.mp4" } }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.IsType<VideoSegment>(body[0]);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a forward segment.</summary>
    [Fact]
    public void ToMessageBody_ForwardSegment()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "forward", ["data"] = new JObject { ["id"] = "fwd123" } }
            };
        MessageBody    body = MessageConverter.ToMessageBody(json);
        ForwardSegment fwd  = (ForwardSegment)body[0];
        Assert.Equal("fwd123", fwd.ForwardId);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts mixed segments.</summary>
    [Fact]
    public void ToMessageBody_MixedSegments()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "text", ["data"] = new JObject { ["text"] = "Hello " } },
                new JObject { ["type"] = "at", ["data"]   = new JObject { ["qq"]   = "12345" } },
                new JObject { ["type"] = "text", ["data"] = new JObject { ["text"] = " world" } }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Equal(3, body.Count);
        Assert.Equal("Hello  world", body.GetText());
        Assert.IsType<MentionSegment>(body[1]);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> drops unknown segments.</summary>
    [Fact]
    public void ToMessageBody_UnknownSegment_Dropped()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "unknown_type", ["data"] = new JObject() }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Empty(body);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a json segment.</summary>
    [Fact]
    public void ToMessageBody_JsonSegment()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "json",
                        ["data"] = new JObject { ["data"] = @"{""app"":""test""}" }
                    }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        LightAppSegment la = (LightAppSegment)body[0];
        Assert.Equal(@"{""app"":""test""}", la.JsonPayload);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts an mface segment.</summary>
    [Fact]
    public void ToMessageBody_MfaceSegment()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "mface",
                        ["data"] = new JObject
                            {
                                ["emoji_package_id"] = 100,
                                ["emoji_id"]         = "emoji123",
                                ["key"]              = "key456",
                                ["summary"]          = "[cute]",
                                ["url"]              = "http://mface.png"
                            }
                    }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        MarketFaceSegment mface = (MarketFaceSegment)body[0];
        Assert.Equal(100L, mface.EmojiPackageId);
        Assert.Equal("emoji123", mface.EmojiId);
        Assert.Equal("key456", mface.Key);
        Assert.Equal("[cute]", mface.Summary);
        Assert.Equal("http://mface.png", mface.Url);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a file segment.</summary>
    [Fact]
    public void ToMessageBody_FileSegment()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "file",
                        ["data"] = new JObject
                            {
                                ["file_id"]   = "f1",
                                ["name"]      = "readme.txt",
                                ["file_size"] = "2048"
                            }
                    }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        FileSegment file = (FileSegment)body[0];
        Assert.Equal("f1", file.FileId);
        Assert.Equal("readme.txt", file.FileName);
        Assert.Equal(2048L, file.FileSize);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts an xml segment.</summary>
    [Fact]
    public void ToMessageBody_XmlSegment()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "xml",
                        ["data"] = new JObject { ["data"] = "<msg>hello</msg>" }
                    }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        XmlSegment xml = (XmlSegment)body[0];
        Assert.Equal("<msg>hello</msg>", xml.XmlPayload);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a markdown segment.</summary>
    [Fact]
    public void ToMessageBody_MarkdownSegment()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "markdown",
                        ["data"] = new JObject { ["content"] = "# Hello World" }
                    }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        MarkdownSegment md = (MarkdownSegment)body[0];
        Assert.Equal("# Hello World", md.Content);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a flash_file segment.</summary>
    [Fact]
    public void ToMessageBody_FlashFileSegment()
    {
        JArray json = new()
            {
                new JObject
                    {
                        ["type"] = "flash_file",
                        ["data"] = new JObject
                            {
                                ["title"]       = "photo.jpg",
                                ["file_set_id"] = "fset1",
                                ["scene_type"]  = 2
                            }
                    }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        FlashFileMessageSegment ff = (FlashFileMessageSegment)body[0];
        Assert.Equal("photo.jpg", ff.Title);
        Assert.Equal("fset1", ff.FileSetId);
        Assert.Equal(2, ff.SceneType);
    }

#endregion

#region Dice and RPS Segment Tests

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a dice segment.</summary>
    [Fact]
    public void ToMessageBody_DiceSegment()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "dice", ["data"] = new JObject { ["result"] = "3" } }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        Assert.IsType<DiceSegment>(body[0]);
        DiceSegment dice = (DiceSegment)body[0];
        Assert.Equal("3", dice.Result);
        Assert.Equal(SegmentType.Face, dice.Type);
        Assert.Equal(SegmentDirection.Both, dice.Direction);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a dice segment with no result.</summary>
    [Fact]
    public void ToMessageBody_DiceSegment_NoResult()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "dice", ["data"] = new JObject() }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        DiceSegment dice = (DiceSegment)body[0];
        Assert.Equal("", dice.Result);
    }

    /// <summary>Verifies dice segment coexists in a MessageBody alongside other segments.</summary>
    [Fact]
    public void ToMessageBody_DiceWithOtherSegments()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "text", ["data"] = new JObject { ["text"]   = "rolled: " } },
                new JObject { ["type"] = "dice", ["data"] = new JObject { ["result"] = "6" } }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Equal(2, body.Count);
        Assert.IsType<TextSegment>(body[0]);
        Assert.IsType<DiceSegment>(body[1]);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts an RPS segment.</summary>
    [Fact]
    public void ToMessageBody_RpsSegment()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "rps", ["data"] = new JObject { ["result"] = "2" } }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        Assert.IsType<RpsSegment>(body[0]);
        RpsSegment rps = (RpsSegment)body[0];
        Assert.Equal("2", rps.Result);
        Assert.Equal(SegmentType.Face, rps.Type);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts an RPS segment with no result.</summary>
    [Fact]
    public void ToMessageBody_RpsSegment_NoResult()
    {
        JArray json = new()
            {
                new JObject { ["type"] = "rps", ["data"] = new JObject() }
            };
        MessageBody body = MessageConverter.ToMessageBody(json);
        Assert.Single(body);
        RpsSegment rps = (RpsSegment)body[0];
        Assert.Equal("", rps.Result);
    }

    /// <summary>Verifies DiceSegment can be added to a MessageBody (direction is Both).</summary>
    [Fact]
    public void MessageBody_CanAddDiceSegment()
    {
        MessageBody body = new();
        body.Add(new DiceSegment());
        Assert.Single(body);
        Assert.IsType<DiceSegment>(body[0]);
    }

    /// <summary>Verifies RpsSegment can be added to a MessageBody (direction is Both).</summary>
    [Fact]
    public void MessageBody_CanAddRpsSegment()
    {
        MessageBody body = new();
        body.Add(new RpsSegment());
        Assert.Single(body);
        Assert.IsType<RpsSegment>(body[0]);
    }

#endregion

#region ToOneBotSegments Tests

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts a text segment.</summary>
    [Fact]
    public void ToOneBotSegments_TextSegment()
    {
        MessageBody         body     = new("hello world");
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("text", segments[0].Type);
        Assert.Equal("hello world", segments[0].Data!.Value<string>("text"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts a mention segment.</summary>
    [Fact]
    public void ToOneBotSegments_MentionSegment()
    {
        MessageBody         body     = new([new MentionSegment { Target = 789L }]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("at", segments[0].Type);
        Assert.Equal("789", segments[0].Data!.Value<string>("qq"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts a mention-all segment.</summary>
    [Fact]
    public void ToOneBotSegments_MentionAll()
    {
        MessageBody         body     = new([new MentionAllSegment()]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Equal("at", segments[0].Type);
        Assert.Equal("all", segments[0].Data!.Value<string>("qq"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts a face segment.</summary>
    [Fact]
    public void ToOneBotSegments_FaceSegment()
    {
        MessageBody         body     = new([new FaceSegment { FaceId = "178" }]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Equal("face", segments[0].Type);
        Assert.Equal("178", segments[0].Data!.Value<string>("id"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts a reply segment.</summary>
    [Fact]
    public void ToOneBotSegments_ReplySegment()
    {
        MessageBody         body     = new([new ReplySegment { TargetId = 42 }]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Equal("reply", segments[0].Type);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts a DiceSegment to OB11 dice.</summary>
    [Fact]
    public void ToOneBotSegments_DiceSegment()
    {
        MessageBody         body     = new([new DiceSegment()]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("dice", segments[0].Type);
    }

    /// <summary>Verifies DiceSegment is sent alone even when mixed with other segments.</summary>
    [Fact]
    public void ToOneBotSegments_DiceSoloSend_DropsOtherSegments()
    {
        MessageBody body = new();
        body.Add(new TextSegment { Text = "rolling..." });
        body.Add(new DiceSegment());
        body.Add(new FaceSegment { FaceId = "1" });
        List<OneBotSegment> result = MessageConverter.ToOneBotSegments(body);
        Assert.Single(result);
        Assert.Equal("dice", result[0].Type);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts an RpsSegment to OB11 rps.</summary>
    [Fact]
    public void ToOneBotSegments_RpsSegment()
    {
        MessageBody         body     = new([new RpsSegment()]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("rps", segments[0].Type);
    }

    /// <summary>Verifies RpsSegment is sent alone even when mixed with other segments.</summary>
    [Fact]
    public void ToOneBotSegments_RpsSoloSend_DropsOtherSegments()
    {
        MessageBody body = new();
        body.Add(new TextSegment { Text = "let's play!" });
        body.Add(new RpsSegment());
        List<OneBotSegment> result = MessageConverter.ToOneBotSegments(body);
        Assert.Single(result);
        Assert.Equal("rps", result[0].Type);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts an ImageSegment.</summary>
    [Fact]
    public void ToOneBotSegments_ImageSegment()
    {
        MessageBody         body     = new([new ImageSegment { FileUri = "http://img.png" }]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("image", segments[0].Type);
        Assert.Equal("http://img.png", segments[0].Data!.Value<string>("file"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts an AudioSegment.</summary>
    [Fact]
    public void ToOneBotSegments_AudioSegment()
    {
        MessageBody         body     = new([new AudioSegment { FileUri = "http://audio.amr" }]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("record", segments[0].Type);
        Assert.Equal("http://audio.amr", segments[0].Data!.Value<string>("file"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts a VideoSegment.</summary>
    [Fact]
    public void ToOneBotSegments_VideoSegment()
    {
        MessageBody         body     = new([new VideoSegment { FileUri = "http://video.mp4" }]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("video", segments[0].Type);
        Assert.Equal("http://video.mp4", segments[0].Data!.Value<string>("file"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts a LightAppSegment.</summary>
    [Fact]
    public void ToOneBotSegments_LightAppSegment()
    {
        MessageBody         body     = new([new LightAppSegment { JsonPayload = @"{""app"":""test""}" }]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("json", segments[0].Type);
        Assert.Equal(@"{""app"":""test""}", segments[0].Data!.Value<string>("data"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts an XmlSegment.</summary>
    [Fact]
    public void ToOneBotSegments_XmlSegment()
    {
        MessageBody         body     = MessageBody.FromIncoming([new XmlSegment { XmlPayload = "<msg>hello</msg>" }]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("xml", segments[0].Type);
        Assert.Equal("<msg>hello</msg>", segments[0].Data!.Value<string>("data"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToOneBotSegments" /> converts a MarkdownSegment.</summary>
    [Fact]
    public void ToOneBotSegments_MarkdownSegment()
    {
        MessageBody         body     = new([new MarkdownSegment { Content = "# Hello" }]);
        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(body);
        Assert.Single(segments);
        Assert.Equal("markdown", segments[0].Type);
        Assert.Equal("# Hello", segments[0].Data!.Value<string>("content"));
    }

#endregion
}