namespace Herald.Core.Tokens;

/// <summary>
/// Where one token stands against the clock.
/// </summary>
public enum TokenState
{
    /// <summary>
    /// The service does not report an expiry.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Expires later than the warning window.
    /// </summary>
    Valid = 1,

    /// <summary>
    /// Expires within the warning window. A warning has been opened.
    /// </summary>
    Expiring = 2,

    /// <summary>
    /// Already expired. Whatever it opens has stopped working.
    /// </summary>
    Expired = 3,
}
