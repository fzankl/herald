namespace Herald.Core.Parsing.YamlDocuments;

/// <summary>
/// One entry of the 'comments' list of a <see cref="PostDocument"/>, before the parser decides
/// whether it names a text or a template.
/// </summary>
internal sealed class CommentDocument
{
    public string? Id { get; set; }

    public string? After { get; set; }

    public string? ScheduledAt { get; set; }

    public string? Text { get; set; }

    public string? Template { get; set; }
}
