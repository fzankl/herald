using Herald.Core.Configuration;
using Herald.Core.Content;
using Herald.Core.Content.GitHub;
using Microsoft.Extensions.Options;
using Refit;

namespace Herald.Core.Test.Unit.Tokens;

public sealed class GitHubTokenExpiryTests
{
    private const string Header = "github-authentication-token-expiration";

    [Fact]
    public async Task GetExpiryAsync___The_Header_As_GitHub_Sends_It___Is_Read()
    {
        var expiry = await Create("2027-01-01 19:06:49 UTC").GetExpiryAsync(TestContext.Current.CancellationToken);

        expiry.Should().Be(new DateTimeOffset(2027, 1, 1, 19, 6, 49, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetExpiryAsync___No_Header___Is_Unknown()
    {
        var expiry = await Create(value: null).GetExpiryAsync(TestContext.Current.CancellationToken);

        expiry.Should().BeNull();
    }

    [Theory]
    [InlineData("2027-01-01T19:06:49Z")]
    [InlineData("whenever")]
    [InlineData("")]
    public async Task GetExpiryAsync___A_Header_In_Another_Shape___Is_Unknown(string value)
    {
        var expiry = await Create(value).GetExpiryAsync(TestContext.Current.CancellationToken);

        expiry.Should().BeNull();
    }

    private static GitHubTokenExpiry Create(string? value)
    {
        var headers = value is null
            ? null
            : new Dictionary<string, string>(StringComparer.Ordinal) { [Header] = value };

        var handler = StubHttpMessageHandler.Returning("{}", headers: headers);
        var options = Options.Create(new ContentRepositoryOptions
        {
            Repository = "owner/blog",
            Token = "token-value",
            PostPattern = "blog/*/linkedin/*.md",
            TemplateFolder = "blog/_series-templates",
        });

        var client = new HttpClient(new GitHubAuthenticationHandler(options) { InnerHandler = handler })
        {
            BaseAddress = new Uri(GitHubContentRepository.BaseAddress),
        };

        return new GitHubTokenExpiry(RestService.ForGenerated<IGitHubApi>(client), options);
    }
}
