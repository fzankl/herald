namespace Herald.Core.Content.GitHub.Requests;

internal sealed class NewIssue
{
    public required string Title { get; set; }

    public required string Body { get; set; }
}
