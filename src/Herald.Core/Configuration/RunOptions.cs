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
    public const string OverdueLimitSettingName = $"{SectionNames.Root}__{nameof(OverdueLimit)}";

    /// <summary>
    /// How long a post stays due after its time has passed. A run that has been down for longer
    /// than this leaves the post alone, so that an outage over a weekend does not publish a week of
    /// posts at once when it comes back. What to do with those is an editorial decision, and the
    /// author makes it by setting a new time.
    /// </summary>
    public static readonly TimeSpan DefaultOverdueLimit = TimeSpan.FromHours(24);

    /// <summary>
    /// Nullable on purpose: it is what separates a missing setting from an explicit
    /// <see cref="RunMode.Dry"/>. With a non-nullable property the default would already be a
    /// valid mode and the "setting is missing" rule could never fire.
    /// </summary>
    public RunMode? Mode { get; init; }

    /// <summary>
    /// Defaults to <see cref="DefaultOverdueLimit"/>. The setting exists for an outage that needs a
    /// wider window once, not because the value is expected to differ per stage.
    /// </summary>
    public TimeSpan OverdueLimit { get; init; } = DefaultOverdueLimit;

    public bool IsLive => Mode is RunMode.Live;
}
