using Herald.Core.Models;

namespace Herald.Core.Publishing;

/// <summary>
/// What one post file turned into on a run. A file that was rejected carries the reasons and no
/// status, so a run can be read without opening the repository.
/// </summary>
public sealed record RunEntry
{
    public required string Slug { get; init; }

    public required string Path { get; init; }

    public PostStatus? Status { get; init; }

    public DateTimeOffset? ScheduledAt { get; init; }

    /// <summary>
    /// Why the file did not become a post. Empty when it did.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
