using FluentValidation;

namespace Herald.Core.Configuration;

/// <summary>
/// Validation rules for <see cref="RunOptions"/>. Every message names the application
/// setting it is about, because a failed start on Flex Consumption offers no other diagnosis.
/// </summary>
public sealed class RunOptionsValidator : AbstractValidator<RunOptions>
{
    private const string Setting = $"Application setting '{RunOptions.ModeSettingName}'";
    private const string OverdueLimitSetting = $"Application setting '{RunOptions.OverdueLimitSettingName}'";
    private const string TokenExpiryWarningSetting = $"Application setting '{RunOptions.TokenExpiryWarningSettingName}'";
    private const string AllowedValues = $"Allowed values: '{nameof(RunMode.Dry)}', '{nameof(RunMode.Live)}'.";

    public RunOptionsValidator()
    {
        RuleFor(options => options.Mode)
            .NotNull()
            .WithMessage($"{Setting} is missing. {AllowedValues}")
            .IsInEnum()
            .WithMessage(options => $"{Setting} has the unsupported value '{options.Mode}'. {AllowedValues}");

        RuleFor(options => options.OverdueLimit)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage(options => $"{OverdueLimitSetting} has the value '{options.OverdueLimit}'. It is how long a post stays due after its time, so it has to be positive. Leave the setting out to use '{RunOptions.DefaultOverdueLimit}'.");

        RuleFor(options => options.TokenExpiryWarning)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage(options => $"{TokenExpiryWarningSetting} has the value '{options.TokenExpiryWarning}'. It is how long before a token expires herald warns, so it has to be positive. Leave the setting out to use '{RunOptions.DefaultTokenExpiryWarning}'.");
    }
}
