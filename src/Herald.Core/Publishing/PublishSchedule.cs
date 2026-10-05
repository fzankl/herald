using Herald.Core.Configuration;
using Herald.Core.Models;
using Microsoft.Extensions.Options;

namespace Herald.Core.Publishing;

/// <summary>
/// Decides what a run would do with a post. It holds the rule and nothing else.
/// </summary>
internal sealed class PublishSchedule
{
    private readonly RunOptions _options;
    private readonly TimeProvider _time;

    public PublishSchedule(TimeProvider time, IOptions<RunOptions> options)
    {
        _options = options.Value;
        _time = time;
    }

    /// <summary>
    /// A target is served when the post is approved, its time has passed and the target carries no
    /// result yet. A result that failed counts as served: it is left alone until the author removes it.
    /// </summary>
    public (PostDue Due, IReadOnlyList<string> Targets) Evaluate(Post post)
    {
        if (post.Status is not PostStatus.Approved)
        {
            return (PostDue.NotApproved, []);
        }

        // Not a bad file but a broken promise: the parser rejects an approved post without a time,
        // so reaching this means the parser changed and this rule was not told.
        if (post.ScheduledAt is not { } scheduledAt)
        {
            throw new InvalidOperationException(
                $"post '{post.Slug}' is approved and carries no 'scheduled_at', which the parser is supposed to reject.");
        }

        var unserved = post.Targets.Where(target => !post.Results.ContainsKey(target)).ToList();

        if (unserved.Count == 0)
        {
            return (PostDue.Served, []);
        }

        var now = _time.GetUtcNow();

        if (scheduledAt > now)
        {
            return (PostDue.Scheduled, []);
        }

        return now - scheduledAt > _options.OverdueLimit
            ? (PostDue.Overdue, [])
            : (PostDue.Due, unserved);
    }
}
