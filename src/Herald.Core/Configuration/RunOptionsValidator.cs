using FluentValidation;

namespace Herald.Core.Configuration;

/// <summary>
/// Validation rules for <see cref="RunOptions"/>. Every message names the application
/// setting it is about, because a failed start on Flex Consumption offers no other diagnosis.
/// </summary>
public sealed class RunOptionsValidator : AbstractValidator<RunOptions>
{
    private const string Setting = $"Application setting '{RunOptions.ModeSettingName}'";
    private const string AllowedValues = $"Allowed values: '{nameof(RunMode.Dry)}', '{nameof(RunMode.Live)}'.";

    public RunOptionsValidator()
    {
        RuleFor(options => options.Mode)
            .NotNull()
            .WithMessage($"{Setting} is missing. {AllowedValues}")
            .IsInEnum()
            .WithMessage(options => $"{Setting} has the unsupported value '{options.Mode}'. {AllowedValues}");
    }
}
