using Herald.Core.Models;
using Herald.Core.Parsing;
using Herald.Core.Writing;

namespace Herald.Core.Test.Unit.Writing;

public sealed class PostWriterTests
{
    // Everything in here that is not the results block belongs to the author: the key order, the
    // single quotes, the comment, the blank line and the unknown fields. None of it may move.
    private const string File = """
        ---
        type: post
        status: approved
        scheduled_at: '2026-09-01T08:00:00+02:00'

        title: "An editorial field herald does not read"
        image: images/a-post.png
        targets: [linkedin]
        ---
        The post text.

        Second paragraph.
        """;

    [Fact]
    public void Write___A_File_Without_Results___Keeps_Every_Other_Line_As_It_Was()
    {
        var written = Write(File, Published());

        written.Should().StartWith(File[..File.IndexOf("---\nThe post text.", StringComparison.Ordinal)]);
        written.Should().EndWith("---\nThe post text.\n\nSecond paragraph.");
        written.Should().Contain("title: \"An editorial field herald does not read\"");
        written.Should().Contain("scheduled_at: '2026-09-01T08:00:00+02:00'");
    }

    [Fact]
    public void Write___A_File_Without_Results___Appends_The_Block_To_The_Front_Matter()
    {
        var written = Write(File, Published());

        written.Should().Contain("""
            results:
              linkedin:
                status: published
            """);
    }

    [Fact]
    public void Write___A_File_With_Results___Replaces_Only_That_Block()
    {
        var before = Write(File, Published("urn:li:activity:1"));

        var after = Write(before, Published("urn:li:activity:2"));

        after.Should().Contain("urn:li:activity:2").And.NotContain("urn:li:activity:1");
        after.Should().Contain("title: \"An editorial field herald does not read\"");
        after.Should().EndWith("---\nThe post text.\n\nSecond paragraph.");
    }

    [Fact]
    public void Write___A_Block_Followed_By_Another_Key___Leaves_That_Key_Alone()
    {
        const string WithKeyAfter = """
            ---
            status: approved
            results:
              linkedin:
                status: published
            title: "comes after the block"
            ---
            The post text.
            """;

        var written = Write(WithKeyAfter, Published("urn:li:activity:2"));

        written.Should().Contain("""title: "comes after the block" """.TrimEnd());
        written.Should().Contain("urn:li:activity:2");
    }

    [Fact]
    public void Write___No_Results_At_All___Leaves_No_Key_Behind()
    {
        var before = Write(File, Published());

        var after = Write(before, new Dictionary<string, TargetResult>(StringComparer.Ordinal));

        after.Should().NotContain("results:");
        after.Should().EndWith("---\nThe post text.\n\nSecond paragraph.");
    }

    [Fact]
    public void Write___A_File_With_Windows_Line_Endings___Writes_Them_Back()
    {
        var written = Write(File.ReplaceLineEndings("\r\n"), Published());

        written.Should().NotContain("\n\n\r\n");
        written.Split("\r\n").Should().HaveCountGreaterThan(10);
        written.Replace("\r\n", "\n", StringComparison.Ordinal).Should().NotContain("\r");
    }

    // The acceptance test of the whole exercise: what was written comes back out of the parser.
    [Fact]
    public void Write___Then_Parse___Returns_The_Results_That_Went_In()
    {
        var results = Published("urn:li:activity:7500432297192607744");

        var post = new PostParser().Parse("2026-09-01-a-post", Write(File, results));

        post.Errors.Should().BeEmpty();
        post.Post!.Results.Should().ContainKey("linkedin");
        post.Post.Results["linkedin"].Urn.Should().Be("urn:li:activity:7500432297192607744");
        post.Post.Results["linkedin"].Status.Should().Be(PublishStatus.Published);
        post.Post.Body.Should().Be("The post text.\n\nSecond paragraph.");
    }

    // The author put the blank line there to separate their keys from herald's block.
    [Fact]
    public void Write___A_Blank_Line_After_The_Block___Keeps_It()
    {
        const string WithBlankLine = "---\nstatus: approved\nresults:\n  linkedin:\n    status: published\n\ntitle: after\n---\nText.";

        var written = Write(WithBlankLine, Published("urn:li:activity:2"));

        written.Should().Contain("\n\ntitle: after");
    }

    [Fact]
    public void Write___No_Results_Where_The_Block_Was_Last___Leaves_No_Blank_Line()
    {
        const string WithBlock = "---\nstatus: approved\nresults:\n  linkedin:\n    status: published\n---\nText.";

        var written = Write(WithBlock, new Dictionary<string, TargetResult>(StringComparer.Ordinal));

        written.Should().Be("---\nstatus: approved\n---\nText.");
    }

    // Two targets must not swap places between runs and produce a commit that changes nothing.
    [Fact]
    public void Write___Two_Targets___Writes_Them_In_A_Stable_Order()
    {
        var results = new Dictionary<string, TargetResult>(StringComparer.Ordinal)
        {
            ["mastodon"] = Published()["linkedin"],
            ["linkedin"] = Published()["linkedin"],
        };

        var written = Write(File, results);

        written.IndexOf("linkedin:", StringComparison.Ordinal)
            .Should().BeLessThan(written.IndexOf("mastodon:", StringComparison.Ordinal));
    }
    private static string Write(string content, IReadOnlyDictionary<string, TargetResult> results)
    {
        var written = new PostWriter().Write(content, results);
        written.ChangesOnlyTheResults.Should().BeTrue();

        return written.Content;
    }

    private static Dictionary<string, TargetResult> Published(string urn = "urn:li:activity:7500432297192607744") =>
        new(StringComparer.Ordinal)
        {
            ["linkedin"] = new()
            {
                Status = PublishStatus.Published,
                Urn = urn,
                PublishedAt = new DateTimeOffset(2026, 9, 1, 8, 0, 10, TimeSpan.FromHours(2)),
            },
        };
}
