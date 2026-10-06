using Herald.Core.Tokens;
using Microsoft.Azure.Functions.Worker;

namespace Herald.Functions.Functions;

/// <summary>
/// Daily timer that warns before an access token expires,
/// by opening an issue in the content repository.
/// </summary>
public sealed class CheckTokenExpiry
{
    private readonly ILogger<CheckTokenExpiry> _logger;
    private readonly ITokenExpiryCheck _check;

    public CheckTokenExpiry(ILogger<CheckTokenExpiry> logger, ITokenExpiryCheck check)
    {
        _logger = logger;
        _check = check;
    }

    [Function(nameof(CheckTokenExpiry))]
    public async Task Run([TimerTrigger("0 0 6 * * *")] TimerInfo _, CancellationToken cancellationToken)
    {
        _logger.Started(nameof(CheckTokenExpiry), DateTimeOffset.UtcNow);

        var report = await _check.RunAsync(cancellationToken);

        _logger.TokensChecked(report.Tokens.Count, report.Warned);

        foreach (var token in report.Tokens.Where(token => token.State is not TokenState.Valid))
        {
            _logger.TokenNeedsAttention(token.Name, token.State, token.ExpiresAt, token.IssueOpened);
        }
    }
}
