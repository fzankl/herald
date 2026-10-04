namespace Herald.Core.Content.GitHub.Responses;

/// <summary>
/// The answer of the git trees API as it arrives, before the blobs are picked out of it.
/// </summary>
internal sealed class TreeResponse
{
    public List<TreeEntry> Tree { get; set; } = [];

    /// <summary>
    /// Set when the repository has more entries than one answer carries.
    /// </summary>
    public bool Truncated { get; set; }
}
