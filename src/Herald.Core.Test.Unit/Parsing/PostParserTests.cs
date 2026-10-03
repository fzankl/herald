using Herald.Core.Models;
using Herald.Core.Parsing;
using Herald.Core.Test.Unit.Extensions;

namespace Herald.Core.Test.Unit.Parsing;

public sealed class PostParserTests
{
    private const string Slug = "2026-09-01-a-post";

    private static readonly PostParser __parser = new();

    [Fact]
    public void Post___Complete_File___Is_Parsed()
    {
        const string Content = """
            ---
            type: post
            status: approved
            scheduled_at: "2026-09-01T08:00:00+02:00"
            image: images/a-post.png
            targets: [linkedin]
            comments:
              - id: article
                after: 0m
                text: |
                  The article: https://example.com
              - id: series
                after: 5m
                template: a-series
            title: "An editorial field herald does not read"
            idea: 43
            results:
              linkedin:
                status: published
                urn: urn:li:activity:7500432297192607744
                published_at: "2026-09-01T08:00:10+02:00"
                comments:
                  article:
                    status: published
                    urn: urn:li:comment:(urn:li:activity:7500432297192607744,7500434085904031744)
                    published_at: "2026-09-01T08:07:17+02:00"
                    hash:
            ---
            The post text.

            Second paragraph.
            """;

        var result = __parser.Parse(Slug, Content);

        result.Errors.Should().BeEmpty();
        result.Post.Should().NotBeNull();

        var post = result.Post!;
        post.Slug.Should().Be(Slug);
        post.Status.Should().Be(PostStatus.Approved);
        post.ScheduledAt.Should().Be(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.FromHours(2)));
        post.Image.Should().Be("images/a-post.png");
        post.Targets.Should().Equal("linkedin");
        post.Body.Should().Be("The post text.\n\nSecond paragraph.");
    }

    [Fact]
    public void Comments___Complete_File___Are_Parsed()
    {
        var post = ParseValid(Comment("""
              - id: article
                after: 2h
                text: |
                  The article: https://example.com
              - id: followup
                scheduled_at: "2026-09-02T09:00:00+02:00"
                template: a-series
            """));

        post.Comments.Should().SatisfyRespectively(
            article =>
            {
                var text = article.Should().BeOfType<TextPostComment>().Subject;
                text.Id.Should().Be("article");
                text.After.Should().Be(TimeSpan.FromHours(2));
                text.ScheduledAt.Should().BeNull();
                text.Text.Should().Be("The article: https://example.com\n");
            },
            followup =>
            {
                var template = followup.Should().BeOfType<TemplatePostComment>().Subject;
                template.Id.Should().Be("followup");
                template.After.Should().Be(TimeSpan.Zero);
                template.ScheduledAt.Should().Be(new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.FromHours(2)));
                template.Template.Should().Be("a-series");
            });
    }

    [Fact]
    public void Results___Complete_File___Are_Parsed()
    {
        var post = ParseValid("""
            ---
            status: approved
            scheduled_at: "2026-09-01T08:00:00+02:00"
            image: images/a-post.png
            targets: [linkedin]
            comments:
              - id: article
                after: 0m
                text: a comment
            results:
              linkedin:
                status: published
                urn: urn:li:share:7500432297192607744
                published_at: "2026-09-01T08:00:10+02:00"
                hash: 3f9a4b2c
                comments:
                  article:
                    status: failed
                    error: "more than 24 hours overdue"
            ---
            The post text.
            """);

        var target = post.Results.Should().ContainKey("linkedin").WhoseValue;
        target.Status.Should().Be(PublishStatus.Published);
        target.Urn.Should().Be("urn:li:share:7500432297192607744");
        target.PublishedAt.Should().Be(new DateTimeOffset(2026, 9, 1, 8, 0, 10, TimeSpan.FromHours(2)));
        target.Hash.Should().Be("3f9a4b2c");

        var comment = target.Comments.Should().ContainKey("article").WhoseValue;
        comment.Status.Should().Be(PublishStatus.Failed);
        comment.Error.Should().Be("more than 24 hours overdue");
        comment.Urn.Should().BeNull();
    }

    [Theory]
    [InlineData("0m", 0)]
    [InlineData("5m", 5)]
    [InlineData("120m", 120)]
    public void Comment___After_In_Minutes___Is_Parsed(string value, int minutes)
    {
        var post = ParseValid(Comment($"""
              - id: article
                after: {value}
                text: a comment
            """));

        post.Comments.Should().ContainSingle()
            .Which.After.Should().Be(TimeSpan.FromMinutes(minutes));
    }

    [Fact]
    public void Comment___Without_A_Time___Follows_The_Post_Immediately()
    {
        var post = ParseValid(Comment("""
              - id: article
                text: a comment
            """));

        var comment = post.Comments.Should().ContainSingle().Subject;
        comment.After.Should().Be(TimeSpan.Zero);
        comment.ScheduledAt.Should().BeNull();
    }

    [Fact]
    public void Post___No_Front_Matter___Is_A_Draft()
    {
        var result = __parser.Parse(Slug, "Just a text without front matter.");

        result.Errors.Should().BeEmpty();
        result.Post.Should().NotBeNull();

        var post = result.Post!;
        post.Status.Should().Be(PostStatus.Draft);
        post.Body.Should().Be("Just a text without front matter.");
    }

    [Fact]
    public void Post___Front_Matter_That_Is_Never_Closed___Is_Rejected()
    {
        var result = __parser.Parse(Slug, """
            ---
            status: draft
            The post text.
            """);

        result.ShouldBeRejectedWith("front matter", "never closed");
    }

    [Fact]
    public void Post___Windows_Line_Endings___Is_Parsed()
    {
        var post = ParseValid("---\r\nstatus: draft\r\n---\r\n\r\nThe post text.");

        post.Status.Should().Be(PostStatus.Draft);
        post.Body.Should().Be("The post text.");
    }

    [Fact]
    public void Post___Front_Matter_Without_Status___Is_A_Draft()
    {
        var post = ParseValid("""
            ---
            title: "Only editorial fields"
            ---
            The post text.
            """);

        post.Status.Should().Be(PostStatus.Draft);
        post.Targets.Should().BeEmpty();
        post.Comments.Should().BeEmpty();
        post.Results.Should().BeEmpty();
    }

    [Fact]
    public void Post___Draft_Without_Image_And_Targets___Is_Parsed()
    {
        var post = ParseValid("""
            ---
            status: draft
            scheduled_at: "2026-10-13T08:00:00+02:00"
            ---
            The post text.
            """);

        post.Status.Should().Be(PostStatus.Draft);
        post.Image.Should().BeNull();
    }

    [Theory]
    [InlineData("Published")]
    [InlineData("published")]
    [InlineData("Draft")]
    public void Post___Unsupported_Status___Is_Rejected(string status)
    {
        var result = __parser.Parse(Slug, $"""
            ---
            status: {status}
            ---
            The post text.
            """);

        result.ShouldBeRejectedWith("'status'", status);
    }

    [Fact]
    public void Post___Unsupported_Type___Is_Rejected()
    {
        var result = __parser.Parse(Slug, """
            ---
            type: article
            status: draft
            ---
            The post text.
            """);

        result.ShouldBeRejectedWith("'type'", "article");
    }

    [Theory]
    [InlineData("2026-09-01T08:00:00")]
    [InlineData("2026-09-01 08:00:00")]
    [InlineData("2026-09-01")]
    public void Post___Scheduled_At_Without_Offset___Is_Rejected(string value)
    {
        var result = __parser.Parse(Slug, $"""
            ---
            status: draft
            scheduled_at: "{value}"
            ---
            The post text.
            """);

        result.ShouldBeRejectedWith("'scheduled_at'", "without a UTC offset");
    }

    [Fact]
    public void Post___Approved_Without_Scheduled_At___Is_Rejected()
    {
        var result = __parser.Parse(Slug, """
            ---
            status: approved
            image: images/a-post.png
            targets: [linkedin]
            ---
            The post text.
            """);

        result.ShouldBeRejectedWith("'scheduled_at'", "is missing");
    }

    [Fact]
    public void Post___Approved_Without_Image___Is_Rejected()
    {
        var result = __parser.Parse(Slug, """
            ---
            status: approved
            scheduled_at: "2026-09-01T08:00:00+02:00"
            targets: [linkedin]
            ---
            The post text.
            """);

        result.ShouldBeRejectedWith("'image'", "is missing");
    }

    [Fact]
    public void Post___Approved_Without_Targets___Is_Rejected()
    {
        var result = __parser.Parse(Slug, """
            ---
            status: approved
            scheduled_at: "2026-09-01T08:00:00+02:00"
            image: images/a-post.png
            ---
            The post text.
            """);

        result.ShouldBeRejectedWith("'targets'", "is missing");
    }

    [Fact]
    public void Comment___Without_Id___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Comment("""
              - after: 0m
                text: a comment
            """));

        result.ShouldBeRejectedWith("'id'", "a comment has no");
    }

    [Fact]
    public void Comment___Duplicate_Id___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Comment("""
              - id: article
                after: 0m
                text: a comment
              - id: article
                after: 5m
                text: another comment
            """));

        result.ShouldBeRejectedWith("comment 'article'", "more than once");
    }

    [Fact]
    public void Comment___Text_And_Template___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Comment("""
              - id: article
                after: 0m
                text: a comment
                template: a-series
            """));

        result.ShouldBeRejectedWith("comment 'article'", "exactly one of 'text' and 'template'");
    }

    [Fact]
    public void Comment___Neither_Text_Nor_Template___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Comment("""
              - id: article
                after: 0m
            """));

        result.ShouldBeRejectedWith("comment 'article'", "exactly one of 'text' and 'template'");
    }

    [Fact]
    public void Comment___After_And_Scheduled_At___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Comment("""
              - id: article
                after: 0m
                scheduled_at: "2026-09-01T09:00:00+02:00"
                text: a comment
            """));

        result.ShouldBeRejectedWith("comment 'article'", "exclusive");
    }

    [Theory]
    [InlineData("5")]
    [InlineData("5 m")]
    [InlineData("5w")]
    [InlineData("-5m")]
    [InlineData("soon")]
    public void Comment___Unsupported_After___Is_Rejected(string value)
    {
        var result = __parser.Parse(Slug, Comment($"""
              - id: article
                after: "{value}"
                text: a comment
            """));

        result.ShouldBeRejectedWith("comment 'article'", "'after'");
    }

    [Theory]
    [InlineData("99999999999m")]
    [InlineData("99999999d")]
    public void Comment___After_Beyond_The_Supported_Range___Is_Rejected(string value)
    {
        var result = __parser.Parse(Slug, Comment($"""
              - id: article
                after: "{value}"
                text: a comment
            """));

        result.ShouldBeRejectedWith("comment 'article'", "'after'");
    }

    [Theory]
    [InlineData("series/a-series")]
    [InlineData("../a-series")]
    [InlineData("a-series.md")]
    public void Comment___Template_With_A_Path___Is_Rejected(string template)
    {
        var result = __parser.Parse(Slug, Comment($"""
              - id: series
                after: 5m
                template: "{template}"
            """));

        result.ShouldBeRejectedWith("comment 'series'", "'template'");
    }

    [Fact]
    public void Result___Unsupported_Status___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Result("""
                status: Published
                urn: urn:li:activity:7500432297192607744
                published_at: "2026-09-01T08:00:10+02:00"
            """));

        result.ShouldBeRejectedWith("target 'linkedin'", "'Published'");
    }

    [Fact]
    public void Result___Published_Without_Urn___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Result("""
                status: published
                published_at: "2026-09-01T08:00:10+02:00"
            """));

        result.ShouldBeRejectedWith("target 'linkedin'", "no 'urn'");
    }

    [Fact]
    public void Result___Published_Without_Published_At___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Result("""
                status: published
                urn: urn:li:activity:7500432297192607744
            """));

        result.ShouldBeRejectedWith("target 'linkedin'", "no 'published_at'");
    }

    [Theory]
    [InlineData("urn:li:fsd_comment:(123,urn:li:activity:456)")]
    [InlineData("7500432297192607744")]
    [InlineData("urn:li:Activity:7500432297192607744")]
    public void Result___Unsupported_Urn___Is_Rejected(string urn)
    {
        var result = __parser.Parse(Slug, Result($"""
                status: published
                urn: "{urn}"
                published_at: "2026-09-01T08:00:10+02:00"
            """));

        result.ShouldBeRejectedWith("target 'linkedin'", urn);
    }

    [Fact]
    public void Result___Failed_Without_Error___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Result("""
                status: failed
            """));

        result.ShouldBeRejectedWith("target 'linkedin'", "no 'error'");
    }

    [Fact]
    public void Comment_Result___Published_Without_Urn___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Result("""
                status: published
                urn: urn:li:activity:7500432297192607744
                published_at: "2026-09-01T08:00:10+02:00"
                comments:
                  article:
                    status: published
                    published_at: "2026-09-01T08:07:17+02:00"
            """));

        result.ShouldBeRejectedWith("comment 'article' of target 'linkedin'", "no 'urn'");
    }

    [Fact]
    public void Comment_Result___Post_Urn___Is_Rejected()
    {
        var result = __parser.Parse(Slug, Result("""
                status: published
                urn: urn:li:activity:7500432297192607744
                published_at: "2026-09-01T08:00:10+02:00"
                comments:
                  article:
                    status: published
                    urn: urn:li:activity:7500434085904031744
                    published_at: "2026-09-01T08:07:17+02:00"
            """));

        result.ShouldBeRejectedWith("comment 'article' of target 'linkedin'", "'urn:li:comment:'");
    }

    [Fact]
    public void Post___Broken_Yaml___Is_Rejected()
    {
        var result = __parser.Parse(Slug, """
            ---
            status: approved
              image: images/a-post.png
            ---
            The post text.
            """);

        result.ShouldBeRejectedWith("front matter", "YAML");
    }

    [Fact]
    public void Post___Several_Problems___Are_All_Reported()
    {
        var result = __parser.Parse(Slug, """
            ---
            status: approved
            scheduled_at: "2026-09-01T08:00:00"
            ---
            The post text.
            """);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Result___A_Target_With_Nothing_Under_It___Is_Not_Served_Yet()
    {
        var result = __parser.Parse(Slug, """
            ---
            status: approved
            scheduled_at: "2026-09-01T08:00:00+02:00"
            image: images/a-post.png
            targets: [linkedin]
            results:
              linkedin:
            ---
            The post text.
            """);

        result.Errors.Should().BeEmpty();
        result.Post!.Results.Should().BeEmpty();
    }

    [Fact]
    public void Comment_Result___A_Comment_With_Nothing_Under_It___Is_Not_Posted_Yet()
    {
        var result = __parser.Parse(Slug, """
            ---
            status: approved
            scheduled_at: "2026-09-01T08:00:00+02:00"
            image: images/a-post.png
            targets: [linkedin]
            results:
              linkedin:
                status: published
                urn: urn:li:activity:7500432297192607744
                published_at: "2026-09-01T08:00:10+02:00"
                comments:
                  article:
            ---
            The post text.
            """);

        result.Errors.Should().BeEmpty();
        result.Post!.Results["linkedin"].Comments.Should().BeEmpty();
    }

    [Fact]
    public void Comment___An_Entry_With_Nothing_Under_It___Is_Rejected()
    {
        var result = __parser.Parse(Slug, """
            ---
            status: draft
            comments:
              -
            ---
            The post text.
            """);

        result.ShouldBeRejectedWith("'id'", "a comment has no");
    }

    private static Post ParseValid(string content)
    {
        var result = __parser.Parse(Slug, content);

        result.Errors.Should().BeEmpty();
        result.Post.Should().NotBeNull();

        return result.Post!;
    }

    private static string Comment(string comments) => $"""
        ---
        status: draft
        scheduled_at: "2026-09-01T08:00:00+02:00"
        comments:
        {comments}
        ---
        The post text.
        """;

    private static string Result(string target) => $"""
        ---
        status: approved
        scheduled_at: "2026-09-01T08:00:00+02:00"
        image: images/a-post.png
        targets: [linkedin]
        results:
          linkedin:
        {target}
        ---
        The post text.
        """;
}
