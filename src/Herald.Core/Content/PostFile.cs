namespace Herald.Core.Content;

/// <summary>
/// One post file of the content repository, before it is read. The slug identifies the post across
/// the repository, and the path is where its text and its write-back live.
/// </summary>
public sealed record PostFile
{
    /// <summary>
    /// The file name without <c>.md</c>, unique across all selected files.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Repository-relative path with <c>/</c> as separator, as the git tree carries it.
    /// </summary>
    public required string Path { get; init; }
}
