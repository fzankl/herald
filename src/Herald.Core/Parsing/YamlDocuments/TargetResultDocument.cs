namespace Herald.Core.Parsing.YamlDocuments;

/// <summary>
/// What herald recorded for one target, keyed by target name in
/// <see cref="PostDocument.Results"/>. The author writes none of these fields.
/// </summary>
internal sealed class TargetResultDocument
{
    public string? Status { get; set; }

    public string? Urn { get; set; }

    public string? PublishedAt { get; set; }

    public string? Error { get; set; }

    public string? Hash { get; set; }

    public SortedDictionary<string, CommentResultDocument>? Comments { get; set; }
}
