using System.Globalization;
using Herald.Core.Configuration;
using Herald.Core.Tokens;
using Microsoft.Extensions.Options;

namespace Herald.Core.Content.GitHub;

/// <inheritdoc />
internal sealed class GitHubTokenExpiry : ITokenExpiry
{
    /// <summary>
    /// GitHub puts the expiry of the token into the answer of every authenticated request.
    /// </summary>
    private const string ExpirationHeader = "github-authentication-token-expiration";

    /// <summary>
    /// The header reads <c>2027-01-01 19:06:49 UTC</c>, which is not ISO 8601:
    /// <see cref="DateTimeOffset.TryParse(string, IFormatProvider, DateTimeStyles, out DateTimeOffset)"/>
    /// returns false on it.
    /// </summary>
    private const string ExpirationFormat = "yyyy-MM-dd HH:mm:ss 'UTC'";

    private readonly IGitHubApi _api;
    private readonly string _owner;
    private readonly string _name;

    public GitHubTokenExpiry(IGitHubApi api, IOptions<ContentRepositoryOptions> options)
    {
        _api = api;

        var segments = options.Value.Repository!.Split('/');
        _owner = segments[0];
        _name = segments[1];
    }

    /// <inheritdoc />
    public string Name => "token of the content repository";

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetExpiryAsync(CancellationToken cancellationToken = default)
    {
        var response = await _api.HeadRepositoryAsync(_owner, _name, cancellationToken).ConfigureAwait(false);

        if (response.Headers is not { } headers || !headers.TryGetValues(ExpirationHeader, out var values))
        {
            // A token without an expiry date sends no header at all, and so does a token type that
            // GitHub does not report on. Neither is a reason to end the run.
            return null;
        }

        var value = values.FirstOrDefault();

        return DateTimeOffset.TryParseExact(
            value,
            ExpirationFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var expiry)
            ? expiry
            : null;
    }
}
