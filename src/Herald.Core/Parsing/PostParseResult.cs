using Herald.Core.Models;

namespace Herald.Core.Parsing;

/// <summary>
/// The outcome of reading one post file. A file either yields a post or a list of reasons why it
/// does not, so that one bad file is reported and skipped instead of ending the run.
/// </summary>
internal sealed record PostParseResult
{
    private PostParseResult(Post? post, IReadOnlyList<string> errors)
    {
        Post = post;
        Errors = errors;
    }

    public Post? Post { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Post is not null;

    public static PostParseResult Parsed(Post post) => new(post, []);

    public static PostParseResult Rejected(IReadOnlyList<string> errors) => new(null, errors);
}
