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

    /// <summary>
    /// How many posts would be published now.
    /// The first number to read before a run goes live.
    /// </summary>
    public int Due => Posts.Count(post => post.Due is PostDue.Due);

    /// <summary>
    /// How many posts herald leaves alone because their time passed too long ago.
    /// </summary>
    public int Overdue => Posts.Count(post => post.Due is PostDue.Overdue);
}
