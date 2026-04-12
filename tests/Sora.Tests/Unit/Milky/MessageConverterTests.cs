using Newtonsoft.Json.Linq;
using Sora.Adapter.Milky.Converter;
using Sora.Adapter.Milky.Models;
using Xunit;

namespace Sora.Tests.Unit.Milky;

/// <summary>Tests for <see cref="MessageConverter" /> (Milky).</summary>
[Collection("Milky.Unit")]
[Trait("Category", "Unit")]
public class MessageConverterTests
{
#region Round-Trip Tests

    /// <summary>Verifies <see cref="MessageConverter" /> round-trips a text message.</summary>
    [Fact]
    public void RoundTrip_TextMessage()
    {
        MessageBody        original      = new("test milky");
        List<MilkySegment> milkySegments = MessageConverter.ToMilkySegments(original);
        MessageBody        restored      = MessageConverter.ToMessageBody(milkySegments);
        Assert.Equal("test milky", restored.GetText());
    }

#endregion

#region ToMessageBody Tests

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a text segment.</summary>
    [Fact]
    public void ToMessageBody_TextSegment()
    {
        List<MilkySegment> segments =
            [
                new() { Type = "text", Data = new JObject { ["text"] = "hello" } }
            ];

        MessageBody body = MessageConverter.ToMessageBody(segments);
        Assert.Single(body);
        Assert.IsType<TextSegment>(body[0]);
        Assert.Equal("hello", ((TextSegment)body[0]).Text);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a mention segment.</summary>
    [Fact]
    public void ToMessageBody_MentionSegment()
    {
        List<MilkySegment> segments =
            [
                new() { Type = "mention", Data = new JObject { ["user_id"] = 12345L, ["name"] = "TestUser" } }
            ];

        MessageBody body = MessageConverter.ToMessageBody(segments);
        Assert.Single(body);
        Assert.IsType<MentionSegment>(body[0]);
        MentionSegment mention = (MentionSegment)body[0];
        Assert.Equal(12345L, (long)mention.Target);
        Assert.Equal("TestUser", mention.Name);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a mention-all segment.</summary>
    [Fact]
    public void ToMessageBody_MentionAllSegment()
    {
        List<MilkySegment> segments = [new() { Type = "mention_all", Data = new JObject() }];
        MessageBody        body     = MessageConverter.ToMessageBody(segments);
        Assert.IsType<MentionAllSegment>(body[0]);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a face segment.</summary>
    [Fact]
    public void ToMessageBody_FaceSegment()
    {
        List<MilkySegment> segments =
            [
                new() { Type = "face", Data = new JObject { ["face_id"] = "123", ["is_large"] = true } }
            ];

        MessageBody body = MessageConverter.ToMessageBody(segments);
        Assert.Single(body);
        FaceSegment face = Assert.IsType<FaceSegment>(body[0]);
        Assert.Equal("123", face.FaceId);
        Assert.True(face.IsLarge);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a reply segment with incoming fields.</summary>
    [Fact]
    public void ToMessageBody_ReplySegment()
    {
        List<MilkySegment> segments =
            [
                new()
                    {
                        Type = "reply",
                        Data = JObject.Parse(
                            @"{
                                ""message_seq"": 12345,
                                ""sender_id"": 100001,
                                ""sender_name"": ""TestUser"",
                                ""time"": 1700000000,
                                ""segments"": [
                                    { ""type"": ""text"", ""data"": { ""text"": ""quoted text"" } }
                                ]
                            }")
                    }
            ];
        MessageBody  body  = MessageConverter.ToMessageBody(segments);
        ReplySegment reply = (ReplySegment)body[0];
        Assert.Equal(12345L, (long)reply.TargetId);
        Assert.Equal(100001L, (long)reply.SenderId);
        Assert.Equal("TestUser", reply.SenderName);
        Assert.Equal(1700000000L, reply.Time);
        Assert.NotNull(reply.Content);
        Assert.Single(reply.Content);
        Assert.IsType<TextSegment>(reply.Content[0]);
        Assert.Equal("quoted text", ((TextSegment)reply.Content[0]).Text);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a reply segment without sender_name.</summary>
    [Fact]
    public void ToMessageBody_ReplySegment_NoSenderName()
    {
        List<MilkySegment> segments =
            [
                new()
                    {
                        Type = "reply",
                        Data = JObject.Parse(
                            @"{
                                ""message_seq"": 99,
                                ""sender_id"": 200002,
                                ""time"": 1700000001,
                                ""segments"": []
                            }")
                    }
            ];
        MessageBody  body  = MessageConverter.ToMessageBody(segments);
        ReplySegment reply = (ReplySegment)body[0];
        Assert.Equal(99L, (long)reply.TargetId);
        Assert.Equal(200002L, (long)reply.SenderId);
        Assert.Null(reply.SenderName);
        Assert.Equal(1700000001L, reply.Time);
        Assert.Null(reply.Content);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts an image segment.</summary>
    [Fact]
    public void ToMessageBody_ImageSegment()
    {
        List<MilkySegment> segments =
            [
                new()
                    {
                        Type = "image",
                        Data = JObject.Parse(
                            @"{""resource_id"": ""img_res_1"", ""temp_url"": ""http://img.png"", ""width"": 100, ""height"": 200, ""sub_type"": ""sticker""}")
                    }
            ];
        MessageBody  body = MessageConverter.ToMessageBody(segments);
        ImageSegment img  = (ImageSegment)body[0];
        Assert.Equal("http://img.png", img.Url);
        Assert.Equal(100, img.Width);
        Assert.Equal(200, img.Height);
        Assert.Equal("img_res_1", img.ResourceId);
        Assert.Equal(ImageSubType.Sticker, img.SubType);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a record segment.</summary>
    [Fact]
    public void ToMessageBody_RecordSegment()
    {
        List<MilkySegment> segments =
            [
                new()
                    {
                        Type = "record",
                        Data = JObject.Parse(@"{""resource_id"": ""aud_res_1"", ""temp_url"": ""http://a.amr"", ""duration"": 5}")
                    }
            ];
        MessageBody  body  = MessageConverter.ToMessageBody(segments);
        AudioSegment audio = (AudioSegment)body[0];
        Assert.Equal(5, audio.Duration);
        Assert.Equal("aud_res_1", audio.ResourceId);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a video segment.</summary>
    [Fact]
    public void ToMessageBody_VideoSegment()
    {
        List<MilkySegment> segments =
            [
                new()
                    {
                        Type = "video",
                        Data = JObject.Parse(
                            @"{""resource_id"": ""vid_res_1"", ""temp_url"": ""http://v.mp4"", ""width"": 1920, ""height"": 1080, ""duration"": 120}")
                    }
            ];
        MessageBody  body = MessageConverter.ToMessageBody(segments);
        VideoSegment vid  = (VideoSegment)body[0];
        Assert.Equal(1920, vid.Width);
        Assert.Equal(120, vid.Duration);
        Assert.Equal("vid_res_1", vid.ResourceId);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a file segment.</summary>
    [Fact]
    public void ToMessageBody_FileSegment()
    {
        List<MilkySegment> segments =
            [
                new()
                    {
                        Type = "file",
                        Data = JObject.Parse(@"{""file_id"": ""f1"", ""file_name"": ""test.txt"", ""file_size"": 1024}")
                    }
            ];
        MessageBody body = MessageConverter.ToMessageBody(segments);
        FileSegment file = (FileSegment)body[0];
        Assert.Equal("f1", file.FileId);
        Assert.Equal(1024L, file.FileSize);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a forward segment.</summary>
    [Fact]
    public void ToMessageBody_ForwardSegment()
    {
        List<MilkySegment> segments =
            [
                new()
                    {
                        Type = "forward",
                        Data = JObject.Parse(
                            @"{""forward_id"": ""fwd1"", ""title"": ""Chat"", ""preview"": [""line1""], ""summary"": ""3 messages""}")
                    }
            ];
        MessageBody    body = MessageConverter.ToMessageBody(segments);
        ForwardSegment fwd  = (ForwardSegment)body[0];
        Assert.Equal("fwd1", fwd.ForwardId);
        Assert.Equal("Chat", fwd.Title);
        Assert.Equal("3 messages", fwd.Summary);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a market-face segment.</summary>
    [Fact]
    public void ToMessageBody_MarketFaceSegment()
    {
        List<MilkySegment> segments =
            [
                new()
                    {
                        Type = "market_face",
                        Data = JObject.Parse(
                            @"{""emoji_package_id"": 1, ""emoji_id"": ""e1"", ""key"": ""k1"", ""summary"": ""[emoji]"", ""url"": ""http://face.png""}")
                    }
            ];
        MessageBody       body = MessageConverter.ToMessageBody(segments);
        MarketFaceSegment mf   = (MarketFaceSegment)body[0];
        Assert.Equal("e1", mf.EmojiId);
        Assert.Equal("[emoji]", mf.Summary);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts a light-app segment.</summary>
    [Fact]
    public void ToMessageBody_LightAppSegment()
    {
        List<MilkySegment> segments =
            [
                new() { Type = "light_app", Data = JObject.Parse(@"{""app_name"": ""MyApp"", ""json_payload"": ""{}""}") }
            ];
        MessageBody     body = MessageConverter.ToMessageBody(segments);
        LightAppSegment app  = (LightAppSegment)body[0];
        Assert.Equal("MyApp", app.AppName);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> converts an XML segment.</summary>
    [Fact]
    public void ToMessageBody_XmlSegment()
    {
        List<MilkySegment> segments =
                [new() { Type = "xml", Data = JObject.Parse(@"{""service_id"": 42, ""xml_payload"": ""<xml/>""}") }];
        MessageBody body = MessageConverter.ToMessageBody(segments);
        XmlSegment  xml  = (XmlSegment)body[0];
        Assert.Equal(42, xml.ServiceId);
        Assert.Equal("<xml/>", xml.XmlPayload);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMessageBody" /> drops unknown segments.</summary>
    [Fact]
    public void ToMessageBody_UnknownSegment_Dropped()
    {
        List<MilkySegment> segments = [new() { Type = "future_type", Data = new JObject() }];
        MessageBody        body     = MessageConverter.ToMessageBody(segments);
        Assert.Empty(body);
    }

#endregion

#region ToMilkySegments Tests

    /// <summary>Verifies <see cref="MessageConverter.ToMilkySegments" /> converts a text segment.</summary>
    [Fact]
    public void ToMilkySegments_TextSegment()
    {
        MessageBody        body     = new("hello milky");
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(body);
        Assert.Single(segments);
        Assert.Equal("text", segments[0].Type);
        Assert.Equal("hello milky", segments[0].Data!.Value<string>("text"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMilkySegments" /> converts a mention-all segment.</summary>
    [Fact]
    public void ToMilkySegments_MentionAll()
    {
        MessageBody        body     = new([new MentionAllSegment()]);
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(body);
        Assert.Equal("mention_all", segments[0].Type);
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMilkySegments" /> converts a face segment.</summary>
    [Fact]
    public void ToMilkySegments_FaceSegment()
    {
        MessageBody        body     = new([new FaceSegment { FaceId = "123", IsLarge = true }]);
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(body);
        Assert.Equal("face", segments[0].Type);
        Assert.Equal("123", segments[0].Data!.Value<string>("face_id"));
        Assert.True(segments[0].Data!.Value<bool>("is_large"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMilkySegments" /> converts a reply segment.</summary>
    [Fact]
    public void ToMilkySegments_ReplySegment()
    {
        MessageBody        body     = new([new ReplySegment { TargetId = 99999L }]);
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(body);
        Assert.Single(segments);
        Assert.Equal("reply", segments[0].Type);
        Assert.Equal(99999L, segments[0].Data!.Value<long>("message_seq"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMilkySegments" /> converts an image segment.</summary>
    [Fact]
    public void ToMilkySegments_ImageSegment()
    {
        MessageBody        body     = new([new ImageSegment { FileUri = "http://send.png", SubType = ImageSubType.Sticker }]);
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(body);
        Assert.Equal("image", segments[0].Type);
        Assert.Equal("http://send.png", segments[0].Data!.Value<string>("uri"));
        Assert.Equal("sticker", segments[0].Data!.Value<string>("sub_type"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMilkySegments" /> converts a mention segment.</summary>
    [Fact]
    public void ToMilkySegments_MentionSegment()
    {
        MessageBody        body     = new([new MentionSegment { Target = 54321L }]);
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(body);
        Assert.Single(segments);
        Assert.Equal("mention", segments[0].Type);
        Assert.Equal(54321L, segments[0].Data!.Value<long>("user_id"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMilkySegments" /> converts an audio segment.</summary>
    [Fact]
    public void ToMilkySegments_AudioSegment()
    {
        MessageBody        body     = new([new AudioSegment { FileUri = "http://audio.amr" }]);
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(body);
        Assert.Single(segments);
        Assert.Equal("record", segments[0].Type);
        Assert.Equal("http://audio.amr", segments[0].Data!.Value<string>("uri"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMilkySegments" /> converts a video segment.</summary>
    [Fact]
    public void ToMilkySegments_VideoSegment()
    {
        MessageBody        body     = new([new VideoSegment { FileUri = "http://video.mp4" }]);
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(body);
        Assert.Single(segments);
        Assert.Equal("video", segments[0].Type);
        Assert.Equal("http://video.mp4", segments[0].Data!.Value<string>("uri"));
    }

    /// <summary>Verifies <see cref="MessageConverter.ToMilkySegments" /> converts a forward segment with messages.</summary>
    [Fact]
    public void ToMilkySegments_ForwardSegment()
    {
        ForwardedMessageNode node = new()
            {
                UserId     = 12345L,
                SenderName = "TestSender",
                Segments   = new MessageBody("hello forward")
            };
        MessageBody body = new(
            [
                new ForwardSegment
                    {
                        Messages = [node],
                        Title    = "ChatLog",
                        Summary  = "1 message",
                        Preview  = ["hello forward"]
                    }
            ]);
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(body);
        Assert.Single(segments);
        Assert.Equal("forward", segments[0].Type);
        Assert.Equal("ChatLog", segments[0].Data!.Value<string>("title"));
        Assert.Equal("1 message", segments[0].Data!.Value<string>("summary"));
        JArray? messages = segments[0].Data!.Value<JArray>("messages");
        Assert.NotNull(messages);
        Assert.Single(messages);
        Assert.Equal(12345L, messages[0].Value<long>("user_id"));
        Assert.Equal("TestSender", messages[0].Value<string>("sender_name"));
    }

#endregion
}