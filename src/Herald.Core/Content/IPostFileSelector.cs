namespace Herald.Core.Content;

/// <summary>
/// Picks the post files out of the paths of the content repository and gives each one its slug.
/// It works on paths alone, so every rule here can be tested with a list of strings.
/// </summary>
internal interface IPostFileSelector
{
    /// <summary>
    /// Selects the post files among <paramref name="paths"/>.
    /// </summary>
    /// <param name="paths">Repository-relative paths, as the git tree carries them.</param>
    /// <returns>The post files, and what was left out.</returns>
    PostFileSelection Select(IEnumerable<string> paths);
}
