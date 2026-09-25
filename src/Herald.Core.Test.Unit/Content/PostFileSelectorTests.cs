using Herald.Core.Configuration;
using Herald.Core.Content;
using Microsoft.Extensions.Options;

namespace Herald.Core.Test.Unit.Content;

public sealed class PostFileSelectorTests
{
    private const string PostPattern = "blog/*/linkedin/*.md";
    private const string TemplateFolder = "blog/_series-templates";

    [Fact]
    public void Posts___Paths_Of_The_Content_Repository___Are_Selected()
    {
        var selection = Select(
            "blog/functions-v1/linkedin/2026-08-18-functions-v1-support-end.md",
            "blog/functions-v1/linkedin/images/a-post.png",
            "blog/functions-v1/linkedin/images/a-post.drawio",
            "blog/functions-v1/index.md",
            "blog/isolated-worker/linkedin/2026-09-01-isolated-worker.md",
            "README.md");

        selection.Warnings.Should().BeEmpty();
        selection.Files.Select(file => file.Path).Should().Equal(
            "blog/functions-v1/linkedin/2026-08-18-functions-v1-support-end.md",
            "blog/isolated-worker/linkedin/2026-09-01-isolated-worker.md");
    }

    [Fact]
    public void Slug___A_Selected_File___Is_The_File_Name_Without_Md()
    {
        var selection = Select("blog/functions-v1/linkedin/2026-08-18-functions-v1-support-end.md");

        selection.Files.Should().ContainSingle()
            .Which.Slug.Should().Be("2026-08-18-functions-v1-support-end");
    }

    [Fact]
    public void Template___Matched_By_The_Post_Pattern___Is_Not_Read_As_A_Post()
    {
        var selection = Select(
            postPattern: "blog/*/*.md",
            paths: ["blog/_series-templates/azure-functions.md", "blog/functions-v1/a-post.md"]);

        selection.Warnings.Should().BeEmpty();
        selection.Files.Select(file => file.Path).Should().Equal("blog/functions-v1/a-post.md");
    }

    [Fact]
    public void Slug___Used_By_Two_Files___Is_Skipped_With_A_Warning()
    {
        var selection = Select(
            "blog/functions-v1/linkedin/a-post.md",
            "blog/isolated-worker/linkedin/a-post.md",
            "blog/functions-v1/linkedin/another-post.md");

        selection.Files.Select(file => file.Path).Should().Equal("blog/functions-v1/linkedin/another-post.md");
        selection.Warnings.Should().ContainSingle()
            .Which.Should().Contain("'a-post'")
            .And.Contain("blog/functions-v1/linkedin/a-post.md")
            .And.Contain("blog/isolated-worker/linkedin/a-post.md");
    }

    [Fact]
    public void Path___Differing_From_The_Pattern_In_Case___Is_Not_Selected()
    {
        var selection = Select("blog/functions-v1/LinkedIn/a-post.md");

        selection.Files.Should().BeEmpty();
        selection.Warnings.Should().BeEmpty();
    }

    private static PostFileSelection Select(params string[] paths) => Select(PostPattern, paths);

    private static PostFileSelection Select(string postPattern, string[] paths) =>
        new PostFileSelector(Options.Create(new ContentRepositoryOptions
        {
            PostPattern = postPattern,
            TemplateFolder = TemplateFolder,
        })).Select(paths);
}
