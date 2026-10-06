using Herald.Core.Configuration;
using Herald.Core.Content.GitHub;
using Herald.Core.Content.GitHub.Requests;
using Microsoft.Extensions.Options;

namespace Herald.Core.Content;

/// <inheritdoc />
internal sealed class GitHubIssueTracker : IIssueTracker
{
    private readonly IGitHubApi _api;
    private readonly string _owner;
    private readonly string _name;

    public GitHubIssueTracker(IGitHubApi api, IOptions<ContentRepositoryOptions> options)
    {
        _api = api;

        var segments = options.Value.Repository!.Split('/');
        _owner = segments[0];
        _name = segments[1];
    }

    /// <inheritdoc />
    public async Task<bool> HasOpenIssueAsync(string title, CancellationToken cancellationToken = default)
    {
        var issues = await _api.GetOpenIssuesAsync(_owner, _name, cancellationToken).ConfigureAwait(false);
        return issues.Any(issue => string.Equals(issue.Title, title, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public Task OpenIssueAsync(string title, string body, CancellationToken cancellationToken = default) =>
        _api.CreateIssueAsync(_owner, _name, new NewIssue { Title = title, Body = body }, cancellationToken);
}
