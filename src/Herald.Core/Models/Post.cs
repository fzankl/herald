namespace Herald.Core.Models;

/// <summary>
/// One post file: what the author wrote in the front matter and the body below it, plus what
/// herald recorded per target.
/// </summary>
public sealed record Post
{
    /// <summary>
    /// The file name without <c>.md</c>, unique across the content repository.
    /// </summary>
    public required string Slug { get; init; }

    public required PostStatus Status { get; init; }

    /// <summary>
    /// When the post is due. Required from <see cref="PostStatus.Approved"/> on.
    /// </summary>
    public DateTimeOffset? ScheduledAt { get; init; }

    /// <summary>
    /// Path of the image, relative to the folder of the post file. Required from
    /// <see cref="PostStatus.Approved"/> on, because a post without an image is not published.
    /// </summary>
    public string? Image { get; init; }

    /// <summary>
    /// The targets the post goes to. Required from <see cref="PostStatus.Approved"/> on and without
    /// a default, so that a target added later never receives posts approved before it existed.
    /// </summary>
    public IReadOnlyList<string> Targets { get; init; } = [];

    public IReadOnlyList<PostComment> Comments { get; init; } = [];

    /// <summary>
    /// What herald recorded, keyed by target name.
    /// </summary>
    public IReadOnlyDictionary<string, TargetResult> Results { get; init; } = new Dictionary<string, TargetResult>(StringComparer.Ordinal);

    /// <summary>
    /// Everything below the front matter, which becomes the post text.
    /// </summary>
    public required string Body { get; init; }
}
