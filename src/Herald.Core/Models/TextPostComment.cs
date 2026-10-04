namespace Herald.Core.Models;

/// <summary>
/// A comment whose text stands in the post file. Once it is posted, herald never changes it again.
/// </summary>
internal sealed record TextPostComment : PostComment
{
    public required string Text { get; init; }
}
