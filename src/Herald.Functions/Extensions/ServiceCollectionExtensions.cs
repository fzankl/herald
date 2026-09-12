using System.Text.Json.Serialization;
using FluentValidation;
using Herald.Core.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Herald.Functions.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates every configuration section of the app. 
    /// </summary>
    public static IServiceCollection AddHeraldOptions(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<HeraldOptions>, HeraldOptionsValidator>();

        services.AddOptions<HeraldOptions>()
            .BindConfiguration(HeraldOptions.SectionName)
            .ValidateWithFluentValidation()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Configurs the app specific JSON settings.
    /// </summary>
    public static IServiceCollection ConfigureHeraldJson(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return services;
    }
}
