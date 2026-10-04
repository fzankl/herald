using System.Reflection;
using Herald.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace Herald.Functions.Functions;

/// <summary>
/// HTTP trigger that answers which code is deployed, and nothing else. The deployment smoke test
/// reads it to decide whether the app it just deployed is the commit it sent, so this function
/// touches no content repository and has nothing that can fail.
/// </summary>
public sealed class ReportVersion
{
    private static readonly string __codeVersion =
        typeof(ReportVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";

    private readonly ILogger<ReportVersion> _logger;
    private readonly RunOptions _options;

    public ReportVersion(ILogger<ReportVersion> logger, IOptions<RunOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    [Function(nameof(ReportVersion))]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest request)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;

        _logger.Started(nameof(ReportVersion), startedAtUtc);

        return new OkObjectResult(new VersionResponse(_options.Mode, __codeVersion, startedAtUtc));
    }

    private sealed record VersionResponse(RunMode? Mode, string Version, DateTimeOffset StartedAtUtc);
}
