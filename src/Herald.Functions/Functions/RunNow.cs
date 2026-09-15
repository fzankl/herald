using System.Reflection;
using Herald.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace Herald.Functions.Functions;

/// <summary>
/// HTTP trigger that will run the publishing pass on demand and return its log. In v0.1 it
/// reports what the app is, which is what the deployment smoke test checks. It is also the only
/// function in the "http" scale group, which makes the grouping visible in the observation.
/// </summary>
public sealed class RunNow
{
    private static readonly string CodeVersion =
        typeof(RunNow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";

    private readonly ILogger<RunNow> _logger;
    private readonly RunOptions _options;

    public RunNow(ILogger<RunNow> logger, IOptions<RunOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    [Function(nameof(RunNow))]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest request)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;

        _logger.Started(nameof(RunNow), startedAtUtc);

        return new OkObjectResult(new RunNowResponse(
            _options.Mode,
            CodeVersion,
            startedAtUtc));
    }

    private sealed record RunNowResponse(
        RunMode? Mode,
        string Version,
        DateTimeOffset StartedAtUtc);
}
