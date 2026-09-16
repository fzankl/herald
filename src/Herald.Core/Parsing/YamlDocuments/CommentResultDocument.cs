namespace Herald.Core.Parsing.YamlDocuments;

/// <summary>
/// What herald recorded for one comment of a target, keyed by the comment id in
/// <see cref="TargetResultDocument.Comments"/>.
/// </summary>
internal sealed class CommentResultDocument
{
    public string? Status { get; set; }

    public string? Urn { get; set; }

    public string? PublishedAt { get; set; }

    public string? Error { get; set; }

    public string? Hash { get; set; }
}
