using Herald.Core.Configuration;
using Herald.Core.Content;
using Herald.Core.Tokens;
using Microsoft.Extensions.Options;

namespace Herald.Core.Test.Unit.Tokens;

public sealed class TokenExpiryCheckTests
{
    private static readonly DateTimeOffset __now = new(2026, 10, 5, 6, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan __warning = TimeSpan.FromDays(30);

    [Fact]
    public async Task RunAsync___A_Token_That_Outlasts_The_Window___Opens_No_Issue()
    {
        var issues = new FakeIssueTracker();

        var report = await Check(issues, Token("the one", __now.AddDays(31)))
            .RunAsync(TestContext.Current.CancellationToken);

        report.Tokens.Should().ContainSingle().Which.State.Should().Be(TokenState.Valid);
        report.Warned.Should().Be(0);
        issues.Opened.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync___A_Token_Inside_The_Window___Opens_An_Issue_That_Names_The_Date()
    {
        var issues = new FakeIssueTracker();

        var report = await Check(issues, Token("token of the content repository", __now.AddDays(29)))
            .RunAsync(TestContext.Current.CancellationToken);

        var token = report.Tokens.Should().ContainSingle().Subject;
        token.State.Should().Be(TokenState.Expiring);
        token.IssueOpened.Should().BeTrue();

        issues.Opened.Should().ContainSingle()
            .Which.Should().Be("herald: the token of the content repository expires on 2026-11-03");
    }

    [Fact]
    public async Task RunAsync___An_Expired_Token___Is_Still_Reported()
    {
        var issues = new FakeIssueTracker();

        var report = await Check(issues, Token("the one", __now.AddDays(-1)))
            .RunAsync(TestContext.Current.CancellationToken);

        report.Tokens.Should().ContainSingle().Which.State.Should().Be(TokenState.Expired);
        report.Warned.Should().Be(1);
        issues.Opened.Should().ContainSingle();
    }

    [Fact]
    public async Task RunAsync___A_Warning_That_Is_Already_Open___Opens_No_Second_Issue()
    {
        var issues = new FakeIssueTracker("herald: the the one expires on 2026-11-03");

        var report = await Check(issues, Token("the one", __now.AddDays(29)))
            .RunAsync(TestContext.Current.CancellationToken);

        report.Tokens.Should().ContainSingle().Which.IssueOpened.Should().BeFalse();
        issues.Opened.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync___A_Token_Whose_Service_Says_Nothing___Is_Unknown()
    {
        var issues = new FakeIssueTracker();

        var report = await Check(issues, Token("the one", expiry: null))
            .RunAsync(TestContext.Current.CancellationToken);

        var token = report.Tokens.Should().ContainSingle().Subject;
        token.State.Should().Be(TokenState.Unknown);
        token.ExpiresAt.Should().BeNull();
        issues.Opened.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync___Several_Tokens___Checks_Every_One()
    {
        var issues = new FakeIssueTracker();

        var report = await Check(
                issues,
                Token("the first", __now.AddDays(100)),
                Token("the second", __now.AddDays(2)))
            .RunAsync(TestContext.Current.CancellationToken);

        report.Tokens.Select(token => token.Name).Should().Equal("the first", "the second");
        report.Warned.Should().Be(1);
        issues.Opened.Should().ContainSingle().Which.Should().Contain("the second");
    }

    private static TokenExpiryCheck Check(IIssueTracker issues, params ITokenExpiry[] tokens) =>
        new(tokens,
            issues,
            new FixedTimeProvider(__now),
            Options.Create(new RunOptions { Mode = RunMode.Dry, TokenExpiryWarning = __warning }));

    private static FakeToken Token(string name, DateTimeOffset? expiry) => new FakeToken(name, expiry);

    private sealed class FakeToken : ITokenExpiry
    {
        private readonly DateTimeOffset? _expiry;

        public FakeToken(string name, DateTimeOffset? expiry)
        {
            Name = name;
            _expiry = expiry;
        }

        public string Name { get; }

        public Task<DateTimeOffset?> GetExpiryAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_expiry);
    }

    private sealed class FakeIssueTracker : IIssueTracker
    {
        private readonly HashSet<string> _open;

        public FakeIssueTracker(params string[] open) => _open = new HashSet<string>(open, StringComparer.Ordinal);

        public List<string> Opened { get; } = [];

        public Task<bool> HasOpenIssueAsync(string title, CancellationToken cancellationToken = default) =>
            Task.FromResult(_open.Contains(title));

        public Task OpenIssueAsync(string title, string body, CancellationToken cancellationToken = default)
        {
            Opened.Add(title);
            _open.Add(title);

            return Task.CompletedTask;
        }
    }
}
