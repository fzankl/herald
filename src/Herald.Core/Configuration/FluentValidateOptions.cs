using FluentValidation;
using Microsoft.Extensions.Options;

namespace Herald.Core.Configuration;

/// <summary>
/// Bridges FluentValidation into the options pipeline so that <c>ValidateOnStart()</c> reports
/// validator failures as plain messages. Rules live in the <see cref="AbstractValidator{T}"/>,
/// never as attributes on the options model.
/// </summary>
/// <typeparam name="TOptions">The options type being validated.</typeparam>
public sealed class FluentValidateOptions<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    private readonly string _name;
    private readonly IValidator<TOptions> _validator;

    public FluentValidateOptions(string name, IValidator<TOptions> validator)
    {
        _name = name;
        _validator = validator;
    }

    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        // A named options instance is validated only
        // by the validator registered for that name.
        if (_name is not null && _name != name)
        {
            return ValidateOptionsResult.Skip;
        }

        ArgumentNullException.ThrowIfNull(options);

        var result = _validator.Validate(options);
        if (result.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = result.Errors.Select(failure => failure.ErrorMessage).ToArray();
        return ValidateOptionsResult.Fail(failures);
    }
}
