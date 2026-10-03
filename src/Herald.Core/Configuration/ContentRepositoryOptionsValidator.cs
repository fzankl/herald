using FluentValidation;

namespace Herald.Core.Configuration;

/// <summary>
/// Validation rules for <see cref="ContentRepositoryOptions"/>. Every message names the application
/// setting it is about, because a failed start on Flex Consumption offers no other diagnosis.
/// </summary>
public sealed class ContentRepositoryOptionsValidator : AbstractValidator<ContentRepositoryOptions>
{
    private const string RepositorySetting = $"Application setting '{ContentRepositoryOptions.RepositorySettingName}'";
    private const string TokenSetting = $"Application setting '{ContentRepositoryOptions.TokenSettingName}'";
    private const string BranchSetting = $"Application setting '{ContentRepositoryOptions.BranchSettingName}'";
    private const string PostPatternSetting = $"Application setting '{ContentRepositoryOptions.PostPatternSettingName}'";
    private const string TemplateFolderSetting = $"Application setting '{ContentRepositoryOptions.TemplateFolderSettingName}'";
    private const string CommitMessageSuffixSetting = $"Application setting '{ContentRepositoryOptions.CommitMessageSuffixSettingName}'";
    private const string RepositoryRelativeRule = "It has to be relative to the repository root, with '/' as separator and without '..'.";

    public ContentRepositoryOptionsValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(options => options.Repository)
            .NotEmpty()
            .WithMessage($"{RepositorySetting} is missing. Example: 'owner/blog'.")
            .Must(repository => IsOwnerAndName(repository!))
            .WithMessage(options => $"{RepositorySetting} has the value '{options.Repository}'. It names the repository as 'owner/name', without a host, without a leading or trailing '/' and without '.git'.");

        // The only rule without a message that quotes the value. A token in a start-up log is a
        // leaked token, so this message names the setting and stops there.
        RuleFor(options => options.Token)
            .NotEmpty()
            .WithMessage($"{TokenSetting} is missing. It holds the access token of the content repository and reaches the app as a key vault reference.");

        RuleFor(options => options.Branch)
            .NotEmpty()
            .WithMessage($"{BranchSetting} is empty. Leave the setting out to use '{ContentRepositoryOptions.DefaultBranch}'.")
            .Must(branch => IsPlainBranch(branch!))
            .WithMessage(options => $"{BranchSetting} has the value '{options.Branch}'. A branch is named without spaces, without '..' and without a leading or trailing '/'.");

        RuleFor(options => options.PostPattern)
            .NotEmpty()
            .WithMessage($"{PostPatternSetting} is missing. Example: 'posts/*/linkedin/*.md'.")
            .Must(pattern => IsRepositoryRelative(pattern!))
            .WithMessage(options => $"{PostPatternSetting} has the value '{options.PostPattern}'. {RepositoryRelativeRule}")
            .Must(pattern => pattern!.EndsWith(".md", StringComparison.Ordinal))
            .WithMessage(options => $"{PostPatternSetting} has the value '{options.PostPattern}'. The pattern has to end with '.md', otherwise it also matches the image sources next to the posts.");

        RuleFor(options => options.TemplateFolder)
            .NotEmpty()
            .WithMessage($"{TemplateFolderSetting} is missing. Example: 'templates'.")
            .Must(folder => IsRepositoryRelative(folder!))
            .WithMessage(options => $"{TemplateFolderSetting} has the value '{options.TemplateFolder}'. {RepositoryRelativeRule}")
            .Must(folder => IsPlainFolder(folder!))
            .WithMessage(options => $"{TemplateFolderSetting} has the value '{options.TemplateFolder}'. The folder is a plain path without wildcards and without a trailing '/'.");

        RuleFor(options => options.CommitMessageSuffix)
            .NotNull()
            .WithMessage($"{CommitMessageSuffixSetting} is null. Leave the setting out to use '{ContentRepositoryOptions.DefaultCommitMessageSuffix}', or set it to an empty value to turn the marker off.");
    }

    private static bool IsRepositoryRelative(string path) =>
        !path.StartsWith('/')
        && !path.Contains('\\')
        && !path.Split('/').Contains("..");

    private static bool IsPlainFolder(string folder) =>
        !folder.EndsWith('/') && !folder.Contains('*');

    // Two segments and nothing else, so that a host, a path or a '.git' suffix is rejected here
    // instead of turning into a request against a repository that does not exist.
    private static bool IsOwnerAndName(string repository)
    {
        var segments = repository.Split('/');

        return segments.Length == 2
            && segments.All(segment => segment.Length > 0 && !segment.Contains(' '))
            && !repository.Contains('\\')
            && !repository.EndsWith(".git", StringComparison.Ordinal);
    }

    private static bool IsPlainBranch(string branch) =>
        !branch.StartsWith('/')
        && !branch.EndsWith('/')
        && !branch.Contains(' ')
        && !branch.Contains('\\')
        && !branch.Split('/').Contains("..");
}
