namespace Herald.Core.Configuration;

/// <summary>
/// How the app runs as a whole, independent of any target or the publishing rules. Bound from the
/// root <c>Herald</c> section. Each more specific area has its own section below it and its own
/// options class, for example <see cref="ContentRepositoryOptions"/>.
/// </summary>
public sealed class RunOptions
{
    public const string SectionName = SectionNames.Root;
    public const string ModeSettingName = $"{SectionNames.Root}__{nameof(Mode)}";

    /// <summary>
    /// Nullable on purpose: it is what separates a missing setting from an explicit
    /// <see cref="RunMode.Dry"/>. With a non-nullable property the default would already be a
    /// valid mode and the "setting is missing" rule could never fire.
    /// </summary>
    public RunMode? Mode { get; init; }

    public bool IsLive => Mode is RunMode.Live;
}
