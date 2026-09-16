namespace Herald.Core.Models;

/// <summary>
/// What herald recorded for one comment on one target, from
/// <c>results.&lt;target&gt;.comments.&lt;id&gt;</c>.
/// </summary>
public sealed record CommentResult
{
    public required PublishStatus Status { get; init; }

    /// <summary>
    /// The id the target returned for the comment. Set when published.
    /// </summary>
    public string? Urn { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>
    /// Why herald stopped. While this is set, herald leaves the comment alone, whatever its status,
    /// so that a permanent error is not retried on every run.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// SHA-256 of the text that was sent. A template comment is rendered again when a post it
    /// references is published, and the hash says whether the result still matches.
    /// </summary>
    public string? Hash { get; init; }
}
