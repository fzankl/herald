using System.Net;
using Herald.Core.Configuration;
using Herald.Core.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Herald.Core.Test.Unit.Content;

public sealed class GitHubContentRepositoryTests
{
    private const string Repository = "owner/blog";
    private const string Token = "token-value";

    private const string Tree = """
        {
          "tree": [
            { "path": "blog/a-post.md", "type": "blob" },
            { "path": "blog", "type": "tree" },
            { "path": "blog/images/a-post.png", "type": "blob" }
          ],
          "truncated": false
        }
        """;

    [Fact]
    public async Task ListPaths___A_Tree___Are_Its_Blobs()
    {
        var (repository, _) = Create(Tree);

        var paths = await repository.ListPathsAsync(TestContext.Current.CancellationToken);

        paths.Should().Equal("blog/a-post.md", "blog/images/a-post.png");
    }

    [Fact]
    public async Task ListPaths___A_Truncated_Tree___Are_Refused()
    {
        var (repository, _) = Create(Tree.Replace("\"truncated\": false", "\"truncated\": true", StringComparison.Ordinal));

        var act = () => repository.ListPathsAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain(Repository).And.Contain("truncated");
    }

    [Fact]
    public async Task Request___Listing_Paths___Asks_For_The_Recursive_Tree_Of_The_Branch()
    {
        var (repository, handler) = Create(Tree);

        await repository.ListPathsAsync(TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.AbsoluteUri.Should()
            .Be($"https://api.github.com/repos/{Repository}/git/trees/main?recursive=1");
    }

    [Fact]
    public async Task Request___Any___Carries_The_Token_The_User_Agent_And_The_Api_Version()
    {
        var (repository, handler) = Create(Tree);

        await repository.ListPathsAsync(TestContext.Current.CancellationToken);

        var headers = handler.LastRequest!.Headers;
        headers.Authorization!.Scheme.Should().Be("Bearer");
        headers.Authorization.Parameter.Should().Be(Token);
        headers.UserAgent.ToString().Should().Be("herald");
        headers.GetValues("X-GitHub-Api-Version").Should().ContainSingle();
    }

    [Fact]
    public async Task Request___Reading_A_File___Escapes_The_Path_That_GitHub_Decodes()
    {
        var (repository, handler) = Create("the post text");

        await repository.ReadTextAsync("blog/a post.md", TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.AbsoluteUri.Should()
            .Be($"https://api.github.com/repos/{Repository}/contents/blog%2Fa%20post.md?ref=main");
    }

    [Fact]
    public async Task ReadText___A_File___Is_The_Body()
    {
        var (repository, _) = Create("the post text");

        var text = await repository.ReadTextAsync("blog/a-post.md", TestContext.Current.CancellationToken);

        text.Should().Be("the post text");
    }

    [Fact]
    public async Task ReadText___A_File_That_Is_Not_There___Throws_With_The_Status_Code()
    {
        var (repository, _) = Create("{}", HttpStatusCode.NotFound);

        var act = () => repository.ReadTextAsync("blog/a-post.md", TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<ApiException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static (IContentRepository Repository, StubHttpMessageHandler Handler) Create(
        string body,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = StubHttpMessageHandler.Returning(body, status);

        // Built through the registration the app uses, with only the innermost handler swapped.
        var services = new ServiceCollection();

        services.AddSingleton(Options.Create(new ContentRepositoryOptions
        {
            Repository = Repository,
            Token = Token,
            PostPattern = "blog/*/linkedin/*.md",
            TemplateFolder = "blog/_series-templates",
        }));

        services.AddHeraldCore(client => client.ConfigurePrimaryHttpMessageHandler(() => handler));

        return (services.BuildServiceProvider().GetRequiredService<IContentRepository>(), handler);
    }
}
