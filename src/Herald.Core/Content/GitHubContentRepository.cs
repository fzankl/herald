using Herald.Core.Configuration;
using Herald.Core.Content.GitHub;
using Microsoft.Extensions.Options;

namespace Herald.Core.Content;

/// <inheritdoc />
internal sealed class GitHubContentRepository : IContentRepository
{
    public const string BaseAddress = "https://api.github.com/";

    private const string BlobType = "blob";

    private readonly IGitHubApi _api;
    private readonly ContentRepositoryOptions _options;
    private readonly string _owner;
    private readonly string _name;

    public GitHubContentRepository(IGitHubApi api, IOptions<ContentRepositoryOptions> options)
    {
        _api = api;
        _options = options.Value;

        var segments = _options.Repository!.Split('/');
        _owner = segments[0];
        _name = segments[1];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListPathsAsync(CancellationToken cancellationToken = default)
    {
        var tree = await _api.GetTreeAsync(_owner, _name, _options.Branch!, cancellationToken).ConfigureAwait(false);

        if (tree.Truncated)
        {
            throw new InvalidOperationException(
                $"the git tree of '{_options.Repository}' is truncated, so the file list is a fragment and is not used.");
        }

        return
        [
            .. tree.Tree
                .Where(entry => entry.Type == BlobType && !string.IsNullOrEmpty(entry.Path))
                .Select(entry => entry.Path!)
        ];
    }

    /// <inheritdoc />
    public Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return _api.GetFileAsync(_owner, _name, path, _options.Branch!, cancellationToken);
    }
}
