namespace Herald.Core.Parsing.YamlDocuments;

/// <summary>
/// The front matter as YAML delivers it, with every value still a string. Validation and the
/// conversion into <see cref="Models.Post"/> happen in <see cref="PostParser"/>, so that a wrong value
/// becomes a message that names the field instead of a deserialization exception.
/// </summary>
internal sealed class PostDocument
{
    public string? Type { get; set; }

    public string? Status { get; set; }

    public string? ScheduledAt { get; set; }

    public string? Image { get; set; }

    public List<string>? Targets { get; set; }

    public List<CommentDocument>? Comments { get; set; }

    public Dictionary<string, TargetResultDocument>? Results { get; set; }
}
