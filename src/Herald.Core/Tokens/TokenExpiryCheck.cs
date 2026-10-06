using System.Globalization;
using Herald.Core.Configuration;
using Herald.Core.Content;
using Microsoft.Extensions.Options;

namespace Herald.Core.Tokens;

/// <inheritdoc />
internal sealed class TokenExpiryCheck : ITokenExpiryCheck
{
    private readonly IEnumerable<ITokenExpiry> _tokens;
    private readonly IIssueTracker _issues;
    private readonly TimeProvider _time;
    private readonly RunOptions _options;

    public TokenExpiryCheck(IEnumerable<ITokenExpiry> tokens,IIssueTracker issues, TimeProvider time, IOptions<RunOptions> options)
    {
        _tokens = tokens;
        _issues = issues;
        _time = time;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<TokenExpiryReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow();
        var entries = new List<TokenExpiryEntry>();

        foreach (var token in _tokens)
        {
            entries.Add(await CheckAsync(token, now, cancellationToken).ConfigureAwait(false));
        }

        return new TokenExpiryReport
        {
            Tokens = entries
        };
    }

    private async Task<TokenExpiryEntry> CheckAsync(ITokenExpiry token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var expiry = await token.GetExpiryAsync(cancellationToken).ConfigureAwait(false);

        if (expiry is not { } expiresAt)
        {
            return new TokenExpiryEntry
            {
                Name = token.Name,
                State = TokenState.Unknown
            };
        }

        var state = expiresAt <= now
            ? TokenState.Expired
            : expiresAt - now <= _options.TokenExpiryWarning
                ? TokenState.Expiring
                : TokenState.Valid;

        if (state is TokenState.Valid)
        {
            return new TokenExpiryEntry
            {
                Name = token.Name,
                ExpiresAt = expiresAt,
                State = state
            };
        }

        // The title carries the date, so a rotated token gets a new issue
        // while a repeated warning about the same one does not.
        var title = $"herald: the {token.Name} expires on {expiresAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        var opened = false;

        if (!await _issues.HasOpenIssueAsync(title, cancellationToken).ConfigureAwait(false))
        {
            await _issues.OpenIssueAsync(title, Body(token, expiresAt), cancellationToken).ConfigureAwait(false);
            opened = true;
        }

        return new TokenExpiryEntry
        {
            Name = token.Name,
            ExpiresAt = expiresAt,
            State = state,
            IssueOpened = opened,
        };
    }

    private static string Body(ITokenExpiry token, DateTimeOffset expiresAt) =>
        $"""
        The {token.Name} expires on {expiresAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} UTC.

        Issue a new one with the same permissions and write it into the key vault of every stage,
        under the same secret name. herald reads the latest version, so no setting has to change.

        This issue was opened by herald and is not closed by it. Close it once the token is replaced.
        """;
}
