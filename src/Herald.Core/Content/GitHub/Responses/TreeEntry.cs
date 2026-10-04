namespace Herald.Core.Content.GitHub.Responses;

/// <summary>
/// One entry of a <see cref="TreeResponse"/>. A recursive tree lists the folders as well, with
/// <c>type</c> <c>tree</c> next to the <c>blob</c> of a file, so an entry is what its type says
/// and not what its path suggests.
/// </summary>
internal sealed class TreeEntry
{
    public string? Path { get; set; }

    public string? Type { get; set; }
}
