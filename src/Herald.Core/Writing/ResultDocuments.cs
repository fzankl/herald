using System.Globalization;
using Herald.Core.Models;
using Herald.Core.Parsing.YamlDocuments;

namespace Herald.Core.Writing;

/// <summary>
/// Turns what herald recorded into the shape the file carries.
/// </summary>
internal static class ResultDocuments
{
    /// <summary>
    /// Seconds and an offset, no fractional part. It is what the content repository already holds,
    /// and the parser requires the offset.
    /// </summary>
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:sszzz";

    /// <summary>
    /// Sorted, and a <see cref="SortedDictionary{TKey,TValue}"/> rather than a sorted
    /// <see cref="Dictionary{TKey,TValue}"/>, whose enumeration order is not promised. Two targets
    /// would otherwise swap places between runs and produce a commit that changes nothing.
    /// </summary>
    internal static SortedDictionary<string, TargetResultDocument> Of(IReadOnlyDictionary<string, TargetResult> results) =>
        results.ToSortedDictionary(
            result => result.Key,
            result => new TargetResultDocument
            {
                Status = Of(result.Value.Status),
                Urn = result.Value.Urn,
                PublishedAt = Of(result.Value.PublishedAt),
                Error = result.Value.Error,
                Hash = result.Value.Hash,
                Comments = result.Value.Comments.Count == 0 ? null : Of(result.Value.Comments),
            },
            StringComparer.Ordinal);

    private static SortedDictionary<string, CommentResultDocument> Of(IReadOnlyDictionary<string, CommentResult> comments) =>
        comments.ToSortedDictionary(
            comment => comment.Key,
            comment => new CommentResultDocument
            {
                Status = Of(comment.Value.Status),
                Urn = comment.Value.Urn,
                PublishedAt = Of(comment.Value.PublishedAt),
                Error = comment.Value.Error,
                Hash = comment.Value.Hash,
            },
            StringComparer.Ordinal);

    private static string Of(PublishStatus status) => status switch
    {
        PublishStatus.Published => "published",
        PublishStatus.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "no name for this status is written to a file."),
    };

    private static string? Of(DateTimeOffset? timestamp) =>
        timestamp?.ToString(TimestampFormat, CultureInfo.InvariantCulture);

    private static SortedDictionary<string, TValue> ToSortedDictionary<TSource, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, string> key,
        Func<TSource, TValue> value,
        StringComparer comparer) =>
        new(source.ToDictionary(key, value, comparer), comparer);
}
