using Microsoft.Azure.Functions.Worker;

namespace Herald.Functions.Functions;

/// <summary>
/// Ten-minute timer that will publish due posts and their comments. In v0.1 it only records
/// that it ran, which is the raw material for the per-function scaling observation.
/// </summary>
public sealed class PublishScheduledPosts
{
    private readonly ILogger<PublishScheduledPosts> _logger;

    public PublishScheduledPosts(ILogger<PublishScheduledPosts> logger) => _logger = logger;

    [Function(nameof(PublishScheduledPosts))]
    public void Run([TimerTrigger("0 */10 * * * *")] TimerInfo timerInfo)
    {
        _logger.Started(nameof(PublishScheduledPosts), DateTimeOffset.UtcNow);
    }
}
