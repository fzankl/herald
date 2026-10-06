namespace Herald.Core.Tokens;

/// <summary>
/// One token of a <see cref="TokenExpiryReport"/>.
/// </summary>
public sealed record TokenExpiryEntry
{
    public required string Name { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public required TokenState State { get; init; }

    /// <summary>
    /// True when this check opened the issue, false when one was already open or none was needed.
    /// </summary>
    public bool IssueOpened { get; init; }
}
