using System.Net;
using Herald.Core.Configuration;
using Herald.Core.Content;
using Herald.Core.Models;
using Herald.Core.Parsing;
using Herald.Core.Publishing;
using Microsoft.Extensions.Options;
using Refit;

namespace Herald.Core.Test.Unit.Publishing;

public sealed class PublishRunTests
{
    // The approved posts below are scheduled for this moment, so they count as due.
    private static readonly DateTimeOffset __now = new(2026, 9, 1, 7, 0, 0, TimeSpan.Zero);

    private const string Approved = """
        ---
        status: approved
        scheduled_at: "2026-09-01T08:00:00+02:00"
        image: images/a-post.png
        targets: [linkedin]
        ---
        The post text.
        """;

    private const string Broken = """
        ---
        status: Published
        ---
        The post text.
        """;

    [Fact]
    public async Task Run___A_Repository_With_Posts___Reports_Each_One()
    {
        var run = Create(new Dictionary<string, string>
        {
            ["blog/a/linkedin/2026-09-01-a-post.md"] = Approved,
            ["blog/a/linkedin/images/a-post.png"] = "not a post",
            ["README.md"] = "not a post either",
        });

        var report = await run.RunAsync(TestContext.Current.CancellationToken);

        report.FilesInRepository.Should().Be(3);
        report.Warnings.Should().BeEmpty();

        var post = report.Posts.Should().ContainSingle().Subject;
        post.Slug.Should().Be("2026-09-01-a-post");
        post.Status.Should().Be(PostStatus.Approved);
        post.Errors.Should().BeEmpty();
        post.Due.Should().Be(PostDue.Due);
        post.Targets.Should().Equal("linkedin");
        post.DueTargets.Should().Equal("linkedin");
        report.Due.Should().Be(1);
    }

    [Fact]
    public async Task Run___A_Rejected_File___Carries_Its_Reasons()
    {
        var run = Create(new Dictionary<string, string>
        {
            ["blog/a/linkedin/2026-09-01-a-post.md"] = Broken,
        });

        var report = await run.RunAsync(TestContext.Current.CancellationToken);

        report.Rejected.Should().Be(1);

        var post = report.Posts.Should().ContainSingle().Subject;
        post.Status.Should().BeNull();
        post.Errors.Should().ContainSingle().Which.Should().Contain("'status'");
    }

    [Fact]
    public async Task Run___A_File_That_Cannot_Be_Read___Reports_It_And_Reads_The_Others()
    {
        var run = Create(
            new Dictionary<string, string>
            {
                ["blog/a/linkedin/2026-09-01-a-post.md"] = Approved,
                ["blog/b/linkedin/2026-09-02-b-post.md"] = Approved,
            },
            unreadable: "blog/a/linkedin/2026-09-01-a-post.md");

        var report = await run.RunAsync(TestContext.Current.CancellationToken);

        report.Posts.Should().HaveCount(2);
        report.Posts.Should().ContainSingle(post => post.Slug == "2026-09-02-b-post")
            .Which.Errors.Should().BeEmpty();
        report.Posts.Should().ContainSingle(post => post.Slug == "2026-09-01-a-post")
            .Which.Errors.Should().ContainSingle().Which.Should().Contain("404");
    }

    private static PublishRun Create(Dictionary<string, string> files, string? unreadable = null)
    {
        var options = Options.Create(new ContentRepositoryOptions
        {
            Repository = "owner/blog",
            Token = "token-value",
            PostPattern = "blog/*/linkedin/*.md",
            TemplateFolder = "blog/_series-templates",
        });

        return new PublishRun(
            new FakeContentRepository(files, unreadable),
            new PostFileSelector(options),
            new PostParser(),
            new PublishSchedule(new FixedTimeProvider(__now), Options.Create(new RunOptions { Mode = RunMode.Dry })));
    }

    private sealed class FakeContentRepository : IContentRepository
    {
        private readonly Dictionary<string, string> _files;
        private readonly string? _unreadable;

        public FakeContentRepository(Dictionary<string, string> files, string? unreadable)
        {
            _files = files;
            _unreadable = unreadable;
        }

        public Task<IReadOnlyList<string>> ListPathsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([.. _files.Keys]);

        public async Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
        {
            if (path == _unreadable)
            {
                throw await ApiException.Create(
                    new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/"),
                    HttpMethod.Get,
                    new HttpResponseMessage(HttpStatusCode.NotFound),
                    new RefitSettings());
            }

            return _files[path];
        }
    }
}
