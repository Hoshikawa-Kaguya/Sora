using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for Segment/MessageBody operator overloads and implicit conversions.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class SegmentOperatorTests
{
#region Implicit Conversion Tests

    /// <see cref="TextSegment" />
    [Fact]
    public void String_ImplicitlyConverts_ToTextSegment()
    {
        TextSegment seg = "hello";
        Assert.Equal("hello", seg.Text);
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void String_ImplicitlyConverts_ToMessageBody()
    {
        MessageBody body = "test message";
        Assert.Single(body);
        Assert.Equal("test message", body.GetText());
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void Segment_ImplicitlyConverts_ToMessageBody()
    {
        MessageBody body = new TextSegment { Text = "hello" };
        Assert.Single(body);
        Assert.Equal("hello", body.GetText());
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void Segment_ImplicitConversion_MentionToBody()
    {
        MessageBody body = new MentionSegment { Target = 123L };
        Assert.Single(body);
        Assert.IsType<MentionSegment>(body[0]);
        Assert.Equal(123L, (long)((MentionSegment)body[0]).Target);
    }

#endregion

#region Segment Operator Tests

    /// <see cref="Segment" />
    [Fact]
    public void Segment_Plus_Segment_ContentAndOrder()
    {
        MessageBody body = new TextSegment { Text = "hello " } + new MentionSegment { Target = 123L };
        Assert.Equal(2, body.Count);
        Assert.Equal("hello ", ((TextSegment)body[0]).Text);
        Assert.Equal(123L, (long)((MentionSegment)body[1]).Target);
    }

    /// <see cref="Segment" />
    [Fact]
    public void Segment_Plus_Segment_DifferentTypes()
    {
        MessageBody body = new ReplySegment { TargetId = 42L } + new FaceSegment { FaceId = "1", IsLarge = true };
        Assert.Equal(2, body.Count);
        Assert.Equal(42L, (long)((ReplySegment)body[0]).TargetId);
        FaceSegment face = (FaceSegment)body[1];
        Assert.Equal("1", face.FaceId);
        Assert.True(face.IsLarge);
    }

    /// <see cref="Segment" />
    [Fact]
    public void Segment_Plus_String_ContentAndOrder()
    {
        MessageBody body = new MentionSegment { Target = 1L } + " welcome!";
        Assert.Equal(2, body.Count);
        Assert.Equal(1L, (long)((MentionSegment)body[0]).Target);
        Assert.Equal(" welcome!", ((TextSegment)body[1]).Text);
    }

    /// <see cref="Segment" />
    [Fact]
    public void String_Plus_Segment_ContentAndOrder()
    {
        MessageBody body = "hello " + new MentionSegment { Target = 1L };
        Assert.Equal(2, body.Count);
        Assert.Equal("hello ", ((TextSegment)body[0]).Text);
        Assert.Equal(1L, (long)((MentionSegment)body[1]).Target);
    }

#endregion

#region MessageBody Operator Tests

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_Plus_Segment_ContentAndOrder()
    {
        MessageBody original = new("hello ");
        MessageBody result   = original + new MentionSegment { Target = 1L };

        Assert.Equal(2, result.Count);
        Assert.Equal("hello ", ((TextSegment)result[0]).Text);
        Assert.Equal(1L, (long)((MentionSegment)result[1]).Target);
        // Original unchanged
        Assert.Single(original);
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_Plus_Segment_MultipleSegments()
    {
        MessageBody original = new([new TextSegment { Text = "a" }, new TextSegment { Text = "b" }]);
        MessageBody result   = original + new TextSegment { Text = "c" };

        Assert.Equal(3, result.Count);
        Assert.Equal("a", ((TextSegment)result[0]).Text);
        Assert.Equal("b", ((TextSegment)result[1]).Text);
        Assert.Equal("c", ((TextSegment)result[2]).Text);
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_Plus_String_ContentAndOrder()
    {
        MessageBody body   = new MessageBody().AddMention(1L);
        MessageBody result = body + " hello";

        Assert.Equal(2, result.Count);
        Assert.Equal(1L, (long)((MentionSegment)result[0]).Target);
        Assert.Equal(" hello", ((TextSegment)result[1]).Text);
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void String_Plus_MessageBody_ContentAndOrder()
    {
        MessageBody body   = new MessageBody().AddMention(1L);
        MessageBody result = "hello " + body;

        Assert.Equal(2, result.Count);
        Assert.Equal("hello ", ((TextSegment)result[0]).Text);
        Assert.Equal(1L, (long)((MentionSegment)result[1]).Target);
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void Segment_Plus_MessageBody_ContentAndOrder()
    {
        MessageBody original = new("world");
        MessageBody result   = new MentionSegment { Target = 1L } + original;

        Assert.Equal(2, result.Count);
        Assert.Equal(1L, (long)((MentionSegment)result[0]).Target);
        Assert.Equal("world", ((TextSegment)result[1]).Text);
        // Original unchanged
        Assert.Single(original);
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_Plus_MessageBody_ContentAndOrder()
    {
        MessageBody left   = new([new TextSegment { Text = "a" }, new MentionSegment { Target = 1L }]);
        MessageBody right  = new([new TextSegment { Text = "b" }, new MentionSegment { Target = 2L }]);
        MessageBody result = left + right;

        Assert.Equal(4, result.Count);
        Assert.Equal("a", ((TextSegment)result[0]).Text);
        Assert.Equal(1L, (long)((MentionSegment)result[1]).Target);
        Assert.Equal("b", ((TextSegment)result[2]).Text);
        Assert.Equal(2L, (long)((MentionSegment)result[3]).Target);
        // Originals unchanged
        Assert.Equal(2, left.Count);
        Assert.Equal(2, right.Count);
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void EmptyBody_Plus_MessageBody()
    {
        MessageBody left   = new();
        MessageBody right  = new("hello");
        MessageBody result = left + right;

        Assert.Single(result);
        Assert.Equal("hello", result.GetText());
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_Plus_EmptyBody()
    {
        MessageBody left   = new("hello");
        MessageBody right  = new();
        MessageBody result = left + right;

        Assert.Single(result);
        Assert.Equal("hello", result.GetText());
    }

#endregion

#region Incoming-Only Handling Tests

    /// <see cref="Segment" />
    [Fact]
    public void IncomingOnlySegment_Plus_SilentlyDropped()
    {
        MarketFaceSegment incoming = new() { EmojiPackageId = 1, EmojiId = "e", Key = "k", Summary = "s", Url = "u" };
        MessageBody       result   = incoming + new TextSegment { Text = "text" };
        Assert.Single(result);
        Assert.Equal("text", result.GetText());
    }

    /// <see cref="Segment" />
    [Fact]
    public void Segment_Plus_IncomingOnlySegment_SilentlyDropped()
    {
        MarketFaceSegment incoming = new() { EmojiPackageId = 1, EmojiId = "e", Key = "k", Summary = "s", Url = "u" };
        MessageBody       result   = new TextSegment { Text = "text" } + incoming;
        Assert.Single(result);
        Assert.Equal("text", result.GetText());
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void MessageBody_Plus_IncomingOnlySegment_SilentlyDropped()
    {
        MessageBody body = new("hello");
        MessageBody result = body
                             + new MarketFaceSegment
                                     { EmojiPackageId = 1, EmojiId = "e", Key = "k", Summary = "s", Url = "u" };
        Assert.Single(result);
        Assert.Equal("hello", result.GetText());
    }

#endregion

#region Chained Operation Tests

    /// <see cref="MessageBody" />
    [Fact]
    public void Chained_Segment_Plus_Segment_Plus_String()
    {
        MessageBody body = new TextSegment { Text = "hello " } + new MentionSegment { Target = 1L } + " how are you?";

        Assert.Equal(3, body.Count);
        Assert.Equal("hello ", ((TextSegment)body[0]).Text);
        Assert.Equal(1L, (long)((MentionSegment)body[1]).Target);
        Assert.Equal(" how are you?", ((TextSegment)body[2]).Text);
    }

    /// <see cref="MessageBody" />
    [Fact]
    public void Chained_Body_Plus_Multiple()
    {
        MessageBody body = new MessageBody("start")
                           + new MentionSegment { Target = 1L }
                           + " middle "
                           + new MentionSegment { Target = 2L }
                           + " end";

        Assert.Equal(5, body.Count);
        Assert.Equal("start", ((TextSegment)body[0]).Text);
        Assert.Equal(1L, (long)((MentionSegment)body[1]).Target);
        Assert.Equal(" middle ", ((TextSegment)body[2]).Text);
        Assert.Equal(2L, (long)((MentionSegment)body[3]).Target);
        Assert.Equal(" end", ((TextSegment)body[4]).Text);
    }

#endregion
}