using System.Net.Http.Headers;
using Herald.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Herald.Core.Content.GitHub;

internal sealed class GitHubAuthenticationHandler : DelegatingHandler
{
    private const string ApiVersionHeader = "X-GitHub-Api-Version";
    private const string ApiVersion = "2022-11-28";

    private readonly ContentRepositoryOptions _options;

    public GitHubAuthenticationHandler(IOptions<ContentRepositoryOptions> options)
    {
        _options = options.Value;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // GitHub answers 403 to a request without a user agent.
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("herald", productVersion: null));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        request.Headers.Add(ApiVersionHeader, ApiVersion);

        return base.SendAsync(request, cancellationToken);
    }
}
