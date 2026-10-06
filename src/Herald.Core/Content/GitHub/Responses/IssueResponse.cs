namespace Herald.Core.Content.GitHub.Responses;

/// <summary>
/// One issue as the api returns it. Only what it takes to recognise an issue herald opened before,
/// so that a warning repeated every day does not become a stack of identical issues.
/// </summary>
internal sealed class IssueResponse
{
    public int Number { get; set; }

    public string? Title { get; set; }
}
