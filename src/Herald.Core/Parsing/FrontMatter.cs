namespace Herald.Core.Parsing;

/// <summary>
/// The block between two delimiter lines at the top of a post file. Finding it is pure text work
/// that knows nothing about posts, and a write-back needs the same rules as a read.
/// </summary>
internal static class FrontMatter
{
    /// <summary>
    /// The line that opens and closes the front matter, as a line of its own.
    /// </summary>
    internal const string Delimiter = "---";

    /// <summary>
    /// Walks the file line by line over the raw text, so that only the front matter and the body are
    /// ever allocated. A post file is a few kilobytes, and its body is most of that.
    /// </summary>
    internal static SplitResultType Split(string content, out string frontMatter, out string body)
    {
        frontMatter = string.Empty;
        body = content;

        var span = content.AsSpan();
        var firstBreak = span.IndexOf('\n');

        if (firstBreak < 0 || !IsDelimiter(span[..firstBreak]))
        {
            return SplitResultType.None;
        }

        var start = firstBreak + 1;

        for (var cursor = start; cursor < span.Length;)
        {
            var lineBreak = span[cursor..].IndexOf('\n');
            var line = lineBreak < 0 ? span[cursor..] : span.Slice(cursor, lineBreak);

            if (!IsDelimiter(line))
            {
                if (lineBreak < 0)
                {
                    break;
                }

                cursor += lineBreak + 1;
                continue;
            }

            // The line break in front of the closing delimiter ends the last line of the front matter.
            var frontMatterEnd = cursor > start ? cursor - 1 : start;
            frontMatter = span[start..frontMatterEnd].ToString();

            // What follows the closing delimiter is the body, less the blank line an author leaves.
            body = lineBreak < 0
                ? string.Empty
                : span[(cursor + lineBreak + 1)..].TrimStart("\r\n").ToString();

            return SplitResultType.Found;
        }

        return SplitResultType.Unterminated;
    }

    private static bool IsDelimiter(ReadOnlySpan<char> line) => line.TrimEnd('\r').SequenceEqual(Delimiter);

    /// <summary>
    /// Splitting file result types.
    /// </summary>
    internal enum SplitResultType
    {
        /// <summary>
        /// The file does not open with a delimiter line, so all of it is body.
        /// </summary>
        None = 0,

        /// <summary>
        /// The file opens with a delimiter line and a second one closes the front matter.
        /// </summary>
        Found = 1,

        /// <summary>
        /// The file opens with a delimiter line that no second one closes.
        /// </summary>
        Unterminated = 2,
    }
}
