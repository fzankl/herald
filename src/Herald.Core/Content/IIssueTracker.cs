namespace Herald.Core.Content;

/// <summary>
/// Where herald leaves a note for a human. The content repository is the one place the author
/// already watches, which is why a warning goes there and not into a log nobody opens.
/// </summary>
internal interface IIssueTracker
{
    /// <summary>
    /// Whether an open issue with exactly this title is already there. A daily warning must not
    /// become a daily issue.
    /// </summary>
    Task<bool> HasOpenIssueAsync(string title, CancellationToken cancellationToken = default);

    Task OpenIssueAsync(string title, string body, CancellationToken cancellationToken = default);
}
