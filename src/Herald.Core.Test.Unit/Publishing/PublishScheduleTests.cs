using Herald.Core.Configuration;
using Herald.Core.Models;
using Herald.Core.Publishing;
using Microsoft.Extensions.Options;

namespace Herald.Core.Test.Unit.Publishing;

public sealed class PublishScheduleTests
{
    // Set here and not left to the default, so that the two tests around the limit state the rule
    // instead of the current preference.
    private static readonly TimeSpan __overdueLimit = TimeSpan.FromHours(24);

    private const string Slug = "2026-10-04-a-post";
    private const string Body = "The post text.";

    private static readonly DateTimeOffset __now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate___Post_Its_Time_Has_Passed___Is_Due_On_Every_Unserved_Target()
    {
        var (due, targets) = Evaluate(CreatePost(__now.AddHours(-1)));

        due.Should().Be(PostDue.Due);
        targets.Should().Equal("linkedin");
    }

    [Fact]
    public void Evaluate___Post_Its_Time_Is_Ahead___Is_Scheduled()
    {
        var (due, targets) = Evaluate(CreatePost(__now.AddMinutes(1)));

        due.Should().Be(PostDue.Scheduled);
        targets.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate___Post_Overdue_Within_The_Limit___Is_Still_Due()
    {
        var (due, _) = Evaluate(CreatePost(__now.AddHours(-23)));

        due.Should().Be(PostDue.Due);
    }

    [Fact]
    public void Evaluate___Post_Overdue_Beyond_The_Limit___Is_Left_Alone()
    {
        var (due, targets) = Evaluate(CreatePost(__now.AddHours(-25)));

        due.Should().Be(PostDue.Overdue);
        targets.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate___A_Draft___Is_Not_Approved()
    {
        var post = new Post { Slug = Slug, Status = PostStatus.Draft, Body = Body };

        var (due, _) = Evaluate(post);

        due.Should().Be(PostDue.NotApproved);
    }

    [Fact]
    public void Evaluate___One_Of_Two_Targets_Carries_A_Result___Only_The_Other_Is_Due()
    {
        var post = CreatePost(__now.AddHours(-1), ["linkedin", "mastodon"], Published("linkedin"));

        var (due, targets) = Evaluate(post);

        due.Should().Be(PostDue.Due);
        targets.Should().Equal("mastodon");
    }

    [Fact]
    public void Evaluate___Every_Target_Carries_A_Result___The_Post_Is_Served()
    {
        var post = CreatePost(__now.AddHours(-1), ["linkedin"], Published("linkedin"));

        var (due, targets) = Evaluate(post);

        due.Should().Be(PostDue.Served);
        targets.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate___A_Failed_Result___Is_Not_Attempted_Again()
    {
        var failed = new Dictionary<string, TargetResult>(StringComparer.Ordinal)
        {
            ["linkedin"] = new() { Status = PublishStatus.Failed, Error = "the target said no" },
        };

        var (due, _) = Evaluate(CreatePost(__now.AddHours(-1), ["linkedin"], failed));

        due.Should().Be(PostDue.Served);
    }

    private static (PostDue Due, IReadOnlyList<string> Targets) Evaluate(Post post) =>
        new PublishSchedule(
            new FixedTimeProvider(__now),
            Options.Create(new RunOptions
            {
                Mode = RunMode.Dry,
                OverdueLimit = __overdueLimit
            }))
            .Evaluate(post);

    private static Post CreatePost(
        DateTimeOffset scheduledAt,
        IReadOnlyList<string>? targets = null,
        IReadOnlyDictionary<string, TargetResult>? results = null) =>
        new()
        {
            Slug = Slug,
            Status = PostStatus.Approved,
            ScheduledAt = scheduledAt,
            Image = "images/a-post.png",
            Targets = targets ?? ["linkedin"],
            Results = results ?? new Dictionary<string, TargetResult>(StringComparer.Ordinal),
            Body = Body,
        };

    private static Dictionary<string, TargetResult> Published(string target) =>
        new(StringComparer.Ordinal)
        {
            [target] = new()
            {
                Status = PublishStatus.Published,
                Urn = "urn:li:activity:7500432297192607744",
                PublishedAt = __now.AddHours(-1),
            },
        };
}
