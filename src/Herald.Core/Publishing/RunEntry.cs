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
    /// What a run would do with the post now.
    /// Null when the file was rejected.
    /// </summary>
    public PostDue? Due { get; init; }

    /// <summary>
    /// The targets the post names. Stands next to <see cref="DueTargets"/> so that an empty one of
    /// those reads as a question of timing and not as a post without a target.
    /// </summary>
    public IReadOnlyList<string> Targets { get; init; } = [];

    /// <summary>
    /// The targets that would be served now. Empty unless <see cref="Due"/> is
    /// <see cref="PostDue.Due"/>.
    /// </summary>
    public IReadOnlyList<string> DueTargets { get; init; } = [];

    /// <summary>
    /// Whether a write-back of what was just read would change more of the file than the results
    /// block. Null when the file was rejected before it got that far. True keeps the post from
    /// becoming due, so that a file herald would damage is never written to.
    /// </summary>
    public bool? WriteBackWouldDamage { get; init; }

    /// <summary>
    /// Why herald does not act on this file. Empty when nothing is in the way.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
