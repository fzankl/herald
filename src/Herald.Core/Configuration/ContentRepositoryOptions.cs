namespace Herald.Core.Configuration;

/// <summary>
/// Where herald finds posts and templates in the content repository, and how it marks its own
/// commits there. Bound from the <c>Herald:Content</c> configuration section.
/// </summary>
public sealed class ContentRepositoryOptions
{
    public const string SectionName = SectionNames.Content;
    public const string DefaultCommitMessageSuffix = "[skip ci]";

    private const string SettingPrefix = $"{SectionNames.Root}__Content__";
    public const string PostPatternSettingName = $"{SettingPrefix}{nameof(PostPattern)}";
    public const string TemplateFolderSettingName = $"{SettingPrefix}{nameof(TemplateFolder)}";
    public const string CommitMessageSuffixSettingName = $"{SettingPrefix}{nameof(CommitMessageSuffix)}";

    /// <summary>
    /// Glob that selects the post files among repository-relative paths, for example
    /// <c>posts/*/linkedin/*.md</c>. A single <c>*</c> does not cross a <c>/</c>.
    /// </summary>
    public string? PostPattern { get; init; }

    /// <summary>
    /// Repository-relative folder that holds the comment templates. A post names a template by its
    /// file name without <c>.md</c>.
    /// </summary>
    public string? TemplateFolder { get; init; }

    /// <summary>
    /// Appended to every commit message herald writes into the content repository. The default
    /// keeps a push workflow in the content repository from building for a status change. An empty
    /// value turns the marker off for a repository that does want the build to run.
    /// </summary>
    public string? CommitMessageSuffix { get; init; } = DefaultCommitMessageSuffix;
}
