using Herald.Core.Content;
using Herald.Core.Parsing;
using Refit;

namespace Herald.Core.Publishing;

/// <inheritdoc />
public sealed class PublishRun : IPublishRun
{
    private readonly IContentRepository _repository;
    private readonly IPostFileSelector _selector;
    private readonly IPostParser _parser;

    public PublishRun(IContentRepository repository, IPostFileSelector selector, IPostParser parser)
    {
        _repository = repository;
        _selector = selector;
        _parser = parser;
    }

    /// <inheritdoc />
    public async Task<RunReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var paths = await _repository.ListPathsAsync(cancellationToken).ConfigureAwait(false);
        var selection = _selector.Select(paths);

        var posts = new List<RunEntry>(selection.Files.Count);

        foreach (var file in selection.Files)
        {
            var post = await ReadAsync(file, cancellationToken).ConfigureAwait(false);
            posts.Add(post);
        }

        return new RunReport
        {
            Posts = posts,
            FilesInRepository = paths.Count,
            Warnings = selection.Warnings,
        };
    }

    private async Task<RunEntry> ReadAsync(PostFile file, CancellationToken cancellationToken)
    {
        string content;

        try
        {
            content = await _repository.ReadTextAsync(file.Path, cancellationToken).ConfigureAwait(false);
        }
        catch (ApiException exception)
        {
            // A file can disappear between the tree and the read, and a single unreadable file is
            // not a reason to leave the other posts unpublished.
            return new RunEntry
            {
                Slug = file.Slug,
                Path = file.Path,
                Errors = [$"the file could not be read: {(int)exception.StatusCode} {exception.StatusCode}."],
            };
        }

        var result = _parser.Parse(file.Slug, content);

        return new RunEntry
        {
            Slug = file.Slug,
            Path = file.Path,
            Status = result.Post?.Status,
            ScheduledAt = result.Post?.ScheduledAt,
            Errors = result.Errors,
        };
    }
}
