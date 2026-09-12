namespace Herald.Core.Configuration;

/// <summary>
/// App-wide switches that are not specific to any target or the publishing rules.
/// Bound from the <c>Herald</c> configuration section, like every other options class.
/// </summary>
public sealed class HeraldOptions
{
    public const string SectionName = "Herald";
    public const string ModeSettingName = $"{SectionName}__{nameof(Mode)}";

    /// <summary>
    /// Nullable on purpose: it is what separates a missing setting from an explicit
    /// <see cref="RunMode.Dry"/>. With a non-nullable property the default would already be a
    /// valid mode and the "setting is missing" rule could never fire.
    /// </summary>
    public RunMode? Mode { get; init; }

    public bool IsLive => Mode is RunMode.Live;
}
