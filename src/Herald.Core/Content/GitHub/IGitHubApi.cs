using Herald.Core.Content.GitHub.Responses;
using Refit;

namespace Herald.Core.Content.GitHub;

/// <summary>
/// The part of the GitHub REST API herald uses. Only the routes live here: the host, the token and
/// the headers every request carries are the business of <see cref="GitHubAuthenticationHandler"/>,
/// and what an answer means is the business of <see cref="GitHubContentRepository"/>.
/// </summary>
internal interface IGitHubApi
{
    /// <summary>
    /// The whole branch in one answer. Without <c>recursive</c> the answer stops at the top folder.
    /// </summary>
    [Get("/repos/{owner}/{name}/git/trees/{branch}?recursive=1")]
    Task<TreeResponse> GetTreeAsync(string owner, string name, string branch, CancellationToken cancellationToken);

    /// <summary>
    /// One file as text. The raw media type returns it as it is, where the default answer would
    /// wrap it in JSON with the content base64 encoded.
    /// </summary>
    [Headers("Accept: application/vnd.github.raw")]
    [Get("/repos/{owner}/{name}/contents/{path}")]
    Task<string> GetFileAsync(
        string owner,
        string name,
        string path,
        [AliasAs("ref")] string branch,
        CancellationToken cancellationToken);
}
