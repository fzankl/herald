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
    /// Walks the file line by line over the raw text and reports where the two parts sit, rather
    /// than copying them out. A reader takes the substrings it needs; a write-back needs the
    /// positions, because it puts the file back together around an untouched remainder.
    /// </summary>
    internal static SplitResultType Split(string content, out Range frontMatter, out Range body)
    {
        frontMatter = 0..0;
        body = 0..content.Length;

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

            // The line break in front of the closing delimiter ends the last line of the front
            // matter, and on a Windows file that break is two characters. Leaving the carriage
            // return inside the range makes a write-back append after half a line break.
            var frontMatterEnd = cursor > start ? cursor - 1 : start;

            if (frontMatterEnd > start && span[frontMatterEnd - 1] == '\r')
            {
                frontMatterEnd--;
            }

            frontMatter = start..frontMatterEnd;

            // What follows the closing delimiter is the body, less the blank line an author leaves.
            var afterDelimiter = lineBreak < 0 ? span.Length : cursor + lineBreak + 1;
            var bodyStart = afterDelimiter;

            while (bodyStart < span.Length && span[bodyStart] is '\r' or '\n')
            {
                bodyStart++;
            }

            body = bodyStart..span.Length;

            return SplitResultType.Found;
        }

        return SplitResultType.Unterminated;
    }

    private static bool IsDelimiter(ReadOnlySpan<char> line) => line.TrimEnd('\r').SequenceEqual(Delimiter);

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
