using System.Globalization;
using System.Text.RegularExpressions;
using Herald.Core.Models;
using Herald.Core.Parsing.YamlDocuments;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Herald.Core.Parsing;

/// <inheritdoc />
public sealed partial class PostParser : IPostParser
{
    private const string TypeValue = "post";

    /// <summary>
    /// The id prefixes a target may return for a post. LinkedIn is the only target so far, and it
    /// answers with a share or ugcPost id, while an existing post carries the activity id from its
    /// post URL.
    /// </summary>
    private static readonly string[] __postUrnPrefixes = ["urn:li:activity:", "urn:li:share:", "urn:li:ugcPost:"];

    /// <summary>
    /// The id prefixes a target may return for a comment.
    /// </summary>
    private static readonly string[] __commentUrnPrefixes = ["urn:li:comment:"];

    private static readonly IDeserializer __deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        // Editorial fields such as title and idea belong to the author, and a write-back keeps them.
        .IgnoreUnmatchedProperties()
        .Build();

    /// <inheritdoc />
    /// <remarks>
    /// A file that collected an error never becomes a Post.
    /// </remarks>
    public PostParseResult Parse(string slug, string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(slug);
        ArgumentNullException.ThrowIfNull(content);

        var splitResult = FrontMatter.Split(content, out var frontMatter, out var body);

        if (splitResult is FrontMatter.SplitResultType.Unterminated)
        {
            return PostParseResult.Rejected([$"the front matter opens with '{FrontMatter.Delimiter}' and is never closed."]);
        }

        if (splitResult is FrontMatter.SplitResultType.None)
        {
            // A file without front matter is a draft, which is what an author starts with.
            return PostParseResult.Parsed(new Post
            {
                Slug = slug,
                Status = PostStatus.Draft,
                Body = content
            });
        }

        PostDocument? document;

        try
        {
            document = __deserializer.Deserialize<PostDocument>(frontMatter);

            if (document is null)
            {
                return PostParseResult.Parsed(new Post
                {
                    Slug = slug,
                    Status = PostStatus.Draft,
                    Body = body
                });
            }
        }
        catch (YamlException exception)
        {
            return PostParseResult.Rejected([$"the front matter is not valid YAML: {exception.Message}"]);
        }

        var errors = new List<string>();

        if (document.Type is not null and not TypeValue)
        {
            errors.Add($"'type' has the unsupported value '{document.Type}'. Allowed value: '{TypeValue}'.");
        }

        var status = ReadStatus(document.Status, errors);
        var scheduledAt = ReadScheduledAt(document.ScheduledAt, status, errors);
        var comments = ReadComments(document.Comments, errors);
        var results = ReadResults(document.Results, errors);

        if (status is PostStatus.Approved)
        {
            if (string.IsNullOrWhiteSpace(document.Image))
            {
                errors.Add("'image' is missing. A post is published with exactly one image.");
            }

            if (document.Targets is null || document.Targets.Count == 0)
            {
                errors.Add("'targets' is missing. From 'approved' on, a post names the targets it goes to.");
            }
        }

        if (document.Targets is not null && document.Targets.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("'targets' contains an empty entry.");
        }

        if (errors.Count > 0)
        {
            return PostParseResult.Rejected(errors);
        }

        return PostParseResult.Parsed(new Post
        {
            Slug = slug,
            // Unreachable past the check above, and Draft is the value that never publishes.
            Status = status ?? PostStatus.Draft,
            ScheduledAt = scheduledAt,
            Image = document.Image,
            Targets = document.Targets ?? [],
            Comments = comments,
            Results = results,
            Body = body,
        });
    }

    /// <remarks>
    /// Missing means draft, so that a file an author has only started is never published.
    /// </remarks>
    private static PostStatus? ReadStatus(string? value, List<string> errors)
    {
        if (value is null)
        {
            return PostStatus.Draft;
        }

        switch (value)
        {
            case "draft":
                return PostStatus.Draft;
            case "approved":
                return PostStatus.Approved;
            default:
                errors.Add($"'status' has the unsupported value '{value}'. Allowed values: 'draft', 'approved'.");
                return null;
        }
    }

    private static DateTimeOffset? ReadTimestamp(string value, string subject, List<string> errors)
    {
        if (!OffsetSuffix().IsMatch(value))
        {
            errors.Add($"{subject} has the value '{value}' without a UTC offset, such as '+02:00' or 'Z'.");
            return null;
        }

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
        {
            errors.Add($"{subject} has the value '{value}', which is not a time in ISO 8601 form.");
            return null;
        }

        return timestamp;
    }

    private static DateTimeOffset? ReadScheduledAt(string? value, PostStatus? status, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (status is PostStatus.Approved)
            {
                errors.Add("'scheduled_at' is missing. From 'approved' on, a post names the time it is due.");
            }

            return null;
        }

        return ReadTimestamp(value, "'scheduled_at'", errors);
    }

    private static List<PostComment> ReadComments(List<CommentDocument>? documents, List<string> errors)
    {
        var comments = new List<PostComment>();

        if (documents is null)
        {
            return comments;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var document in documents)
        {
            if (string.IsNullOrWhiteSpace(document.Id))
            {
                errors.Add("a comment has no 'id'. The id is how its result is recorded.");
                continue;
            }

            var id = document.Id;

            if (!visited.Add(id))
            {
                errors.Add($"comment '{id}': the id is used more than once in this post.");
                continue;
            }

            if (document.After is not null && document.ScheduledAt is not null)
            {
                errors.Add($"comment '{id}': 'after' and 'scheduled_at' are exclusive.");
            }

            // A comment that names no time at all follows the post immediately.
            var after = ReadAfter(document.After, id, errors) ?? TimeSpan.Zero;
            var scheduledAt = document.ScheduledAt is null
                ? null
                : ReadTimestamp(document.ScheduledAt, $"'scheduled_at' of comment '{id}'", errors);

            var comment = ReadComment(document, id, errors);
            if (comment is null)
            {
                continue;
            }

            comments.Add(comment with
            {
                After = after,
                ScheduledAt = scheduledAt
            });
        }

        return comments;
    }

    private static PostComment? ReadComment(CommentDocument document, string id, List<string> errors)
    {
        switch (document)
        {
            case { Text: not null, Template: null }:
                return new TextPostComment { Id = id, Text = document.Text };

            case { Text: null, Template: not null } when IsPlainTemplateName(document.Template):
                return new TemplatePostComment { Id = id, Template = document.Template };

            case { Text: null, Template: not null }:
                errors.Add($"comment '{id}': 'template' has the value '{document.Template}'. A template is named by its file name in the template folder, without '.md' and without a path.");
                return null;

            default:
                errors.Add($"comment '{id}': exactly one of 'text' and 'template' is expected.");
                return null;
        }
    }

    private static TimeSpan? ReadAfter(string? value, string commentId, List<string> errors)
    {
        if (value is null)
        {
            return null;
        }

        var match = AfterValue().Match(value);
        if (!match.Success)
        {
            errors.Add($"comment '{commentId}': 'after' has the value '{value}'. Expected a number followed by 'm', 'h' or 'd', such as '5m'.");
            return null;
        }

        var amount = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return match.Groups[2].Value switch
        {
            "m" => TimeSpan.FromMinutes(amount),
            "h" => TimeSpan.FromHours(amount),
            _ => TimeSpan.FromDays(amount),
        };
    }

    private static Dictionary<string, TargetResult> ReadResults(Dictionary<string, TargetResultDocument>? documents, List<string> errors)
    {
        var results = new Dictionary<string, TargetResult>(StringComparer.Ordinal);

        if (documents is null)
        {
            return results;
        }

        foreach (var (target, document) in documents)
        {
            var status = ReadPublishStatus(document.Status, $"of target '{target}'", errors);
            if (status is null)
            {
                continue;
            }

            var publishedAt = ReadResultTimestamp(document.PublishedAt, $"'published_at' of target '{target}'", errors);
            ValidateResult(status.Value, document.Urn, publishedAt, document.Error, $"target '{target}'", __postUrnPrefixes, errors);

            results[target] = new TargetResult
            {
                Status = status.Value,
                Urn = document.Urn,
                PublishedAt = publishedAt,
                Error = document.Error,
                Hash = document.Hash,
                Comments = ReadCommentResults(document.Comments, target, errors),
            };
        }

        return results;
    }

    private static Dictionary<string, CommentResult> ReadCommentResults(
        Dictionary<string, CommentResultDocument>? documents,
        string target,
        List<string> errors)
    {
        var results = new Dictionary<string, CommentResult>(StringComparer.Ordinal);

        if (documents is null)
        {
            return results;
        }

        foreach (var (id, document) in documents)
        {
            var subject = $"comment '{id}' of target '{target}'";

            var status = ReadPublishStatus(document.Status, $"of {subject}", errors);
            if (status is null)
            {
                continue;
            }

            var publishedAt = ReadResultTimestamp(document.PublishedAt, $"'published_at' of {subject}", errors);
            ValidateResult(status.Value, document.Urn, publishedAt, document.Error, subject, __commentUrnPrefixes, errors);

            results[id] = new CommentResult
            {
                Status = status.Value,
                Urn = document.Urn,
                PublishedAt = publishedAt,
                Error = document.Error,
                Hash = document.Hash,
            };
        }

        return results;
    }

    private static PublishStatus? ReadPublishStatus(string? value, string subject, List<string> errors)
    {
        switch (value)
        {
            case "published":
                return PublishStatus.Published;
            case "failed":
                return PublishStatus.Failed;
            case null:
                errors.Add($"the result {subject} has no 'status'. Allowed values: 'published', 'failed'.");
                return null;
            default:
                errors.Add($"the result {subject} has the unsupported status '{value}'. Allowed values: 'published', 'failed'.");
                return null;
        }
    }

    private static DateTimeOffset? ReadResultTimestamp(string? value, string subject, List<string> errors) =>
        string.IsNullOrWhiteSpace(value) ? null : ReadTimestamp(value, subject, errors);

    private static void ValidateResult(
        PublishStatus status,
        string? urn,
        DateTimeOffset? publishedAt,
        string? error,
        string subject,
        string[] urnPrefixes,
        List<string> errors)
    {
        if (status is PublishStatus.Published)
        {
            if (string.IsNullOrWhiteSpace(urn))
            {
                errors.Add($"{subject} is published and carries no 'urn'.");
            }
            else if (!urnPrefixes.Any(prefix => urn.StartsWith(prefix, StringComparison.Ordinal)))
            {
                errors.Add($"{subject} has the urn '{urn}'. Expected one of {string.Join(", ", urnPrefixes.Select(prefix => $"'{prefix}'"))}.");
            }

            if (publishedAt is null)
            {
                errors.Add($"{subject} is published and carries no 'published_at'.");
            }
        }
        else if (string.IsNullOrWhiteSpace(error))
        {
            errors.Add($"{subject} has failed and carries no 'error'.");
        }
    }

    private static bool IsPlainTemplateName(string template) =>
        !string.IsNullOrWhiteSpace(template)
        && !template.Contains('/')
        && !template.Contains('\\')
        && template != ".."
        && !template.EndsWith(".md", StringComparison.Ordinal);

    [GeneratedRegex(@"(Z|[+-]\d{2}:?\d{2})$")]
    private static partial Regex OffsetSuffix();

    // At most seven digits, because a longer number of days overflows TimeSpan and a longer number
    // of anything overflows int. A value that long is a typo, and it gets the 'after' message
    // instead of an exception that would end the run over one bad file.
    [GeneratedRegex(@"^(\d{1,7})([mhd])$")]
    private static partial Regex AfterValue();
}
