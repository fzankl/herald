namespace Herald.Core.Publishing;

/// <summary>
/// What one run found. It is the whole answer: nothing is published without appearing here,
/// which is what makes a dry run worth reading.
/// </summary>
public sealed record RunReport
{
    /// <summary>
    /// Files on the branch, before the post pattern is applied.
    /// </summary>
    public required int FilesInRepository { get; init; }

    public required IReadOnlyList<RunEntry> Posts { get; init; }

    /// <summary>
    /// What the selection left out, such as a slug two files share.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    public int Rejected => Posts.Count(post => post.Errors.Count > 0);
}
