using Microsoft.Azure.Functions.Worker;

namespace Herald.Functions.Functions;

/// <summary>
/// Daily timer that will warn before the access tokens expire. Its schedule is part
/// of the observation: whether a once-a-day timer fires reliably on Flex Consumption 
/// is one of the questions this app is meant to answer, so every start is logged.
/// </summary>
public sealed class CheckTokenExpiry
{
    private readonly ILogger<CheckTokenExpiry> _logger;

    public CheckTokenExpiry(ILogger<CheckTokenExpiry> logger) => _logger = logger;

    [Function(nameof(CheckTokenExpiry))]
    public void Run([TimerTrigger("0 0 6 * * *")] TimerInfo timerInfo)
    {
        _logger.Started(nameof(CheckTokenExpiry), DateTimeOffset.UtcNow);
    }
}
