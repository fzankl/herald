using FluentValidation;

namespace Herald.Core.Configuration;

/// <summary>
/// Validation rules for <see cref="ContentRepositoryOptions"/>. Every message names the application
/// setting it is about, because a failed start on Flex Consumption offers no other diagnosis.
/// </summary>
public sealed class ContentRepositoryOptionsValidator : AbstractValidator<ContentRepositoryOptions>
{
    private const string PostPatternSetting = $"Application setting '{ContentRepositoryOptions.PostPatternSettingName}'";
    private const string TemplateFolderSetting = $"Application setting '{ContentRepositoryOptions.TemplateFolderSettingName}'";
    private const string CommitMessageSuffixSetting = $"Application setting '{ContentRepositoryOptions.CommitMessageSuffixSettingName}'";
    private const string RepositoryRelativeRule = "It has to be relative to the repository root, with '/' as separator and without '..'.";

    public ContentRepositoryOptionsValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

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
}
