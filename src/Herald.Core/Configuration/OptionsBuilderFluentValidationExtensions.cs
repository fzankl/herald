using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Herald.Core.Configuration;

public static class OptionsBuilderFluentValidationExtensions
{
    /// <summary>
    /// Registers the <see cref="IValidator{T}"/> resolved from the container as the options
    /// validator for <typeparamref name="TOptions"/>. Combine with <c>ValidateOnStart()</c> so a
    /// misconfigured deployment fails at start-up instead of at the first timer run.
    /// </summary>
    public static OptionsBuilder<TOptions> ValidateWithFluentValidation<TOptions>(
        this OptionsBuilder<TOptions> optionsBuilder)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(serviceProvider =>
            new FluentValidateOptions<TOptions>(
                optionsBuilder.Name,
                serviceProvider.GetRequiredService<IValidator<TOptions>>()));

        return optionsBuilder;
    }
}
