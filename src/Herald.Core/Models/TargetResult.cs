namespace Herald.Core.Models;

/// <summary>
/// What herald recorded for one target, from <c>results.&lt;target&gt;</c>. Every field in here
/// belongs to herald, and a write-back replaces the block as a whole.
/// </summary>
internal sealed record TargetResult
{
    public required PublishStatus Status { get; init; }

    /// <summary>
    /// The id the target returned for the post. Set when published.
    /// </summary>
    public string? Urn { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>
    /// Why herald stopped. While this is set, herald leaves the target alone, whatever its status.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// SHA-256 of the text that was sent. It records what actually went out, and a file whose text
    /// no longer matches it has been edited after publication. herald reports that and changes
    /// nothing, because a post is public and rewriting it is an editorial decision. Empty for a post
    /// that was published by hand, where there is nothing to compare against.
    /// </summary>
    public string? Hash { get; init; }

    /// <summary>
    /// Results per comment id. A comment without an entry has not been posted yet.
    /// </summary>
    public IReadOnlyDictionary<string, CommentResult> Comments { get; init; } =
        new Dictionary<string, CommentResult>(StringComparer.Ordinal);
}
