namespace Herald.Core.Configuration;

public enum RunMode
{
    /// <summary>
    /// Nothing is published and nothing is written back to the content repository.
    /// </summary>
    Dry = 0,

    /// <summary>
    /// Posts are published and results are committed back to the content repository.
    /// </summary>
    /// <remarks>
    /// Deliberately not a small integer. The configuration binder also accepts numeric values for
    /// enums, so with Live = 1 an application setting of Herald__Mode=1 - a plausible mistake for
    /// anyone who reads the setting as an on/off flag - would silently start publishing.
    /// </remarks>
    Live = 1000,
}
