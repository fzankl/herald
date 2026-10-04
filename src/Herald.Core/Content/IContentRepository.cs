namespace Herald.Core.Content;

/// <summary>
/// Read access to the content repository. It knows about files and paths and nothing about posts,
/// so what it returns is what <see cref="IPostFileSelector"/> and the parser work on.
/// </summary>
internal interface IContentRepository
{
    /// <summary>
    /// The repository-relative paths of every file on the configured branch, in one request.
    /// </summary>
    Task<IReadOnlyList<string>> ListPathsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The text of one file on the configured branch, named by the repository-relative path that
    /// <see cref="ListPathsAsync"/> returned.
    /// </summary>
    Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default);
}
