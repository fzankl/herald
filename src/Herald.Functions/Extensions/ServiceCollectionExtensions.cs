using System.Text.Json.Serialization;
using FluentValidation;
using Herald.Core.Configuration;
using Herald.Core.Content;
using Herald.Core.Parsing;
using Microsoft.AspNetCore.Mvc;

namespace Herald.Functions.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates every configuration section of the app. 
    /// </summary>
    public static IServiceCollection AddHeraldOptions(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<RunOptions>, RunOptionsValidator>();

        services.AddOptions<RunOptions>()
            .BindConfiguration(RunOptions.SectionName)
            .ValidateWithFluentValidation()
            .ValidateOnStart();

        services.AddSingleton<IValidator<ContentRepositoryOptions>, ContentRepositoryOptionsValidator>();

        services.AddOptions<ContentRepositoryOptions>()
            .BindConfiguration(ContentRepositoryOptions.SectionName)
            .ValidateWithFluentValidation()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Registers the domain services of Herald.Core. They are singletons because they hold no state
    /// and depend on nothing that lives for one function invocation.
    /// </summary>
    public static IServiceCollection AddHeraldServices(this IServiceCollection services)
    {
        services.AddSingleton<IPostParser, PostParser>();
        services.AddSingleton<IPostFileSelector, PostFileSelector>();

        return services;
    }

    /// <summary>
    /// Configures the app specific JSON settings.
    /// </summary>
    public static IServiceCollection ConfigureHeraldJson(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return services;
    }
}
