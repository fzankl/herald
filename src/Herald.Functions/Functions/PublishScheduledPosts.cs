using Herald.Core.Publishing;
using Microsoft.Azure.Functions.Worker;

namespace Herald.Functions.Functions;

/// <summary>
/// Ten-minute timer that will publish due posts and their comments.
/// </summary>
public sealed class PublishScheduledPosts
{
    private readonly ILogger<PublishScheduledPosts> _logger;
    private readonly IPublishRun _publishRun;

    public PublishScheduledPosts(IPublishRun publishRun, ILogger<PublishScheduledPosts> logger)
    {
        _publishRun = publishRun;
        _logger = logger;
    }

    [Function(nameof(PublishScheduledPosts))]
    public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo _, CancellationToken cancellationToken)
    {
        _logger.Started(nameof(PublishScheduledPosts), DateTimeOffset.UtcNow);

        var report = await _publishRun.RunAsync(cancellationToken);

        _logger.RunFinished(
            report.FilesInRepository,
            report.Posts.Count,
            report.DueCount,
            report.OverdueCount,
            report.RejectedCount,
            report.Warnings.Count);

        foreach (var post in report.Posts.Where(post => post.Errors.Count > 0))
        {
            _logger.PostRejected(post.Slug, string.Join(" ", post.Errors));
        }

        foreach (var warning in report.Warnings)
        {
            _logger.SelectionWarning(warning);
        }
    }
}
