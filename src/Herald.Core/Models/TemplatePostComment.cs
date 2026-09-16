namespace Herald.Core.Models;

/// <summary>
/// A comment rendered from a template, such as the list of a series. herald renders it again when a
/// post it references is published, and edits the comment when the rendered text has changed.
/// </summary>
public sealed record TemplatePostComment : PostComment
{
    /// <summary>
    /// Name of the template file in the template folder, without <c>.md</c> and without a path.
    /// </summary>
    public required string Template { get; init; }
}
