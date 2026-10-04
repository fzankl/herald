namespace Herald.Core.Models;

/// <summary>
/// A comment the author plans below their own post. Its text comes either from the file or from a
/// template, which is why the two cases are two types: <see cref="TextPostComment"/> and
/// <see cref="TemplatePostComment"/>. A comment carries no result, because what herald published
/// lives in <see cref="TargetResult.Comments"/>, keyed by <see cref="Id"/>.
/// </summary>
internal abstract record PostComment
{
    private protected PostComment()
    {
    }

    /// <summary>
    /// Freely chosen, unique within the post, and the key of the comment's results.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// How long after the post went live the comment follows, from <c>0m</c>, <c>5m</c>, <c>2h</c>
    /// or <c>3d</c>. A comment that names no time at all follows immediately, which is why this is
    /// not nullable. Ignored when <see cref="ScheduledAt"/> is set.
    /// </summary>
    public TimeSpan After { get; init; }

    /// <summary>
    /// An absolute time instead of <see cref="After"/>.
    /// </summary>
    public DateTimeOffset? ScheduledAt { get; init; }
}
