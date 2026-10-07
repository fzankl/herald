using Herald.Core.Models;
using Herald.Core.Parsing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Herald.Core.Writing;

/// <inheritdoc />
internal sealed class PostWriter : IPostWriter
{
    private const string Key = "results";
    private const string Indent = "  ";

    /// <summary>
    /// Only the results block goes through the serializer, never the whole document: a round trip
    /// through YamlDotNet would return the author's keys in its own order, with its own quoting and
    /// without the comments.
    /// </summary>
    private static readonly ISerializer __serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    /// <inheritdoc />
    public WriteResult Write(string content, IReadOnlyDictionary<string, TargetResult> results)
    {
        if (FrontMatter.Split(content, out var frontMatterRange, out _) is not FrontMatter.SplitResultType.Found)
        {
            // A file without front matter is a draft and carries no results. Handing it back
            // unchanged is the honest answer. Dropping a result into it would not be.
            return results.Count == 0
                ? new WriteResult(content, ChangesOnlyTheResults: true)
                : throw new InvalidOperationException("a file without front matter has nowhere to keep a result.");
        }

        var frontMatter = content[frontMatterRange];
        var block = ResultsBlock.Find(frontMatter);
        var lineEnding = LineEndingOf(frontMatter);

        var rewritten = string.Concat(
            frontMatter[..block.Start.Value],
            Render(results, lineEnding, leadingBreak: block.Start.Value > 0),
            frontMatter[block.End.Value..]);

        var written = string.Concat(
            content[..frontMatterRange.Start.Value],
            rewritten,
            content[frontMatterRange.End.Value..]);

        return new WriteResult(written, ChangesOnlyTheResults(content, written));
    }

    private static string Render(IReadOnlyDictionary<string, TargetResult> results, string lineEnding, bool leadingBreak)
    {
        if (results.Count == 0)
        {
            // Nothing recorded means no key at all, rather than an empty mapping the parser would
            // then have to read as "not served yet".
            return string.Empty;
        }

        var body = __serializer.Serialize(ResultDocuments.Of(results)).ReplaceLineEndings(lineEnding).TrimEnd('\r', '\n');
        var indented = string.Join(
            lineEnding,
            body.Split(lineEnding).Select(line => line.Length == 0 ? line : Indent + line));

        // The line break behind the block belongs to the text that follows and is never written
        // here, which is what keeps the author's spacing after it.
        return string.Concat(
            leadingBreak ? lineEnding : string.Empty,
            Key,
            ":",
            lineEnding,
            indented);
    }

    private static string LineEndingOf(string frontMatter)
    {
        var index = frontMatter.IndexOf('\n', StringComparison.Ordinal);

        return index > 0 && frontMatter[index - 1] == '\r' ? "\r\n" : "\n";
    }

    private static bool ChangesOnlyTheResults(string original, string written)
    {
        if (FrontMatter.Split(original, out var before, out _) is not FrontMatter.SplitResultType.Found
            || FrontMatter.Split(written, out var after, out _) is not FrontMatter.SplitResultType.Found)
        {
            // Nothing was written into a file without front matter, so nothing can have been lost.
            return string.Equals(original, written, StringComparison.Ordinal);
        }

        return string.Equals(original[..before.Start.Value], written[..after.Start.Value], StringComparison.Ordinal)
            && string.Equals(original[before.End.Value..], written[after.End.Value..], StringComparison.Ordinal)
            && SameAroundTheBlock(original[before], written[after]);
    }

    private static bool SameAroundTheBlock(string original, string written)
    {
        var originalBlock = ResultsBlock.Find(original);
        var writtenBlock = ResultsBlock.Find(written);

        return string.Equals(original[..originalBlock.Start.Value], written[..writtenBlock.Start.Value], StringComparison.Ordinal)
            && string.Equals(original[originalBlock.End.Value..], written[writtenBlock.End.Value..], StringComparison.Ordinal);
    }
}
