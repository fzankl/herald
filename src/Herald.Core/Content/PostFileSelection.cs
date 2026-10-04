namespace Herald.Core.Content;

/// <summary>
/// The post files of one content repository.
/// </summary>
internal sealed record PostFileSelection
{
    internal PostFileSelection(IReadOnlyList<PostFile> files, IReadOnlyList<string> warnings)
    {
        Files = files;
        Warnings = warnings;
    }

    /// <summary>
    /// The selected files, each with a slug no other file shares.
    /// </summary>
    public IReadOnlyList<PostFile> Files { get; }

    /// <summary>
    /// What was left out and why. A warning never stops the run, because the other posts are still
    /// due.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }
}
