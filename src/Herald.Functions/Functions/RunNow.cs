using Herald.Core.Publishing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Herald.Functions.Functions;

/// <summary>
/// HTTP trigger that carries out a run on demand and returns its report.
/// It is what makes a local run possible without waiting for the timer.
/// </summary>
public sealed class RunNow
{
    private readonly ILogger<RunNow> _logger;
    private readonly IPublishRun _publishRun;

    public RunNow(ILogger<RunNow> logger, IPublishRun publishRun)
    {
        _logger = logger;
        _publishRun = publishRun;
    }

    [Function(nameof(RunNow))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        _logger.Started(nameof(RunNow), DateTimeOffset.UtcNow);

        return new OkObjectResult(await _publishRun.RunAsync(cancellationToken));
    }
}
