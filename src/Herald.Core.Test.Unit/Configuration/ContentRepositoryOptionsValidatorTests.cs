using Herald.Core.Configuration;
using Herald.Core.Test.Unit.Extensions;
using Microsoft.Extensions.Configuration;

namespace Herald.Core.Test.Unit.Configuration;

public sealed class ContentRepositoryOptionsValidatorTests
{
    private const string PostPattern = "posts/*/linkedin/*.md";
    private const string TemplateFolder = "templates";

    private readonly ContentRepositoryOptionsValidator _validator = new();

    [Fact]
    public void Options___All_Settings_Valid___Passes()
    {
        var result = _validator.Validate(CreateOptions());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Options___Missing_Post_Pattern___Fails(string? postPattern)
    {
        var result = _validator.Validate(CreateOptions(postPattern: postPattern));

        result.ShouldHaveSingleFailureFor(ContentRepositoryOptions.PostPatternSettingName, "is missing");
    }

    [Theory]
    [InlineData("/posts/*/linkedin/*.md")]
    [InlineData(@"posts\*\linkedin\*.md")]
    [InlineData("../posts/*/linkedin/*.md")]
    public void Options___Post_Pattern_Outside_Repository_Root___Fails(string postPattern)
    {
        var result = _validator.Validate(CreateOptions(postPattern: postPattern));

        result.ShouldHaveSingleFailureFor(ContentRepositoryOptions.PostPatternSettingName, "relative to the repository root");
    }

    [Fact]
    public void Options___Post_Pattern_Without_Md_Extension___Fails()
    {
        var result = _validator.Validate(CreateOptions(postPattern: "posts/*/linkedin/*"));

        result.ShouldHaveSingleFailureFor(ContentRepositoryOptions.PostPatternSettingName, "has to end with '.md'");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Options___Missing_Template_Folder___Fails(string? templateFolder)
    {
        var result = _validator.Validate(CreateOptions(templateFolder: templateFolder));

        result.ShouldHaveSingleFailureFor(ContentRepositoryOptions.TemplateFolderSettingName, "is missing");
    }

    [Theory]
    [InlineData("/templates")]
    [InlineData(@"posts\templates")]
    [InlineData("posts/../templates")]
    public void Options___Template_Folder_Outside_Repository_Root___Fails(string templateFolder)
    {
        var result = _validator.Validate(CreateOptions(templateFolder: templateFolder));

        result.ShouldHaveSingleFailureFor(ContentRepositoryOptions.TemplateFolderSettingName, "relative to the repository root");
    }

    [Theory]
    [InlineData("templates/")]
    [InlineData("templates/*")]
    public void Options___Template_Folder_Not_A_Plain_Path___Fails(string templateFolder)
    {
        var result = _validator.Validate(CreateOptions(templateFolder: templateFolder));

        result.ShouldHaveSingleFailureFor(ContentRepositoryOptions.TemplateFolderSettingName, "plain path");
    }

    [Fact]
    public void Options___Null_Commit_Message_Suffix___Fails()
    {
        var result = _validator.Validate(CreateOptions(commitMessageSuffix: null));

        result.ShouldHaveSingleFailureFor(ContentRepositoryOptions.CommitMessageSuffixSettingName, "is null");
    }

    [Fact]
    public void Options___Empty_Commit_Message_Suffix___Passes()
    {
        var result = _validator.Validate(CreateOptions(commitMessageSuffix: ""));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Configuration___Missing_Commit_Message_Suffix___Binds_Default()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["Herald:Content:PostPattern"] = PostPattern,
            ["Herald:Content:TemplateFolder"] = TemplateFolder,
        });

        options.CommitMessageSuffix.Should().Be(ContentRepositoryOptions.DefaultCommitMessageSuffix);
    }

    [Fact]
    public void Configuration___Empty_Commit_Message_Suffix___Binds_Empty_Suffix()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["Herald:Content:PostPattern"] = PostPattern,
            ["Herald:Content:TemplateFolder"] = TemplateFolder,
            ["Herald:Content:CommitMessageSuffix"] = "",
        });

        options.CommitMessageSuffix.Should().BeEmpty();
        _validator.Validate(options).IsValid.Should().BeTrue();
    }

    private static ContentRepositoryOptions CreateOptions(
        string? postPattern = PostPattern,
        string? templateFolder = TemplateFolder,
        string? commitMessageSuffix = ContentRepositoryOptions.DefaultCommitMessageSuffix) =>
        new()
        {
            PostPattern = postPattern,
            TemplateFolder = templateFolder,
            CommitMessageSuffix = commitMessageSuffix,
        };

    private static ContentRepositoryOptions Bind(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build()
            .GetSection(ContentRepositoryOptions.SectionName)
            .Get<ContentRepositoryOptions>()!;
}
