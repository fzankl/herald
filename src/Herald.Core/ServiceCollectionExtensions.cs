using Herald.Core.Content;
using Herald.Core.Content.GitHub;
using Herald.Core.Parsing;
using Herald.Core.Publishing;
using Herald.Core.Tokens;
using Herald.Core.Writing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refit;

namespace Herald.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything Herald.Core offers. <paramref name="configureContentClient"/> is for tests:
    /// it replaces the innermost handler of the content client, so a test exercises this registration
    /// rather than one of its own.
    /// </summary>
    /// <remarks>
    /// The parser and the selector are singletons because they hold no state and depend on nothing that
    /// lives for one invocation. The content repository is not: it sits on a Refit client, and a client
    /// the factory hands out must not be captured by a singleton, or its handler is never rotated.
    /// The run holds the repository, so it cannot outlive it either.
    /// </remarks>
    public static IServiceCollection AddHeraldCore(
        this IServiceCollection services,
        Action<IHttpClientBuilder>? configureContentClient = null)
    {
        services.AddSingleton<IPostParser, PostParser>();
        services.AddSingleton<IPostFileSelector, PostFileSelector>();

        services.AddTransient<GitHubAuthenticationHandler>();

        var contentClient = services.AddRefitGeneratedClient<IGitHubApi>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(GitHubContentRepository.BaseAddress))
            .AddHttpMessageHandler<GitHubAuthenticationHandler>();

        configureContentClient?.Invoke(contentClient);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<PublishSchedule>();
        services.AddTransient<IContentRepository, GitHubContentRepository>();
        services.AddSingleton<IPostWriter, PostWriter>();
        services.AddTransient<IPublishRun, PublishRun>();

        services.AddTransient<IIssueTracker, GitHubIssueTracker>();
        services.AddTransient<ITokenExpiry, GitHubTokenExpiry>();
        services.AddTransient<ITokenExpiryCheck, TokenExpiryCheck>();

        return services;
    }
}
