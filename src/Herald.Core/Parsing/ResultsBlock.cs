namespace Herald.Core.Parsing;

/// <summary>
/// Where the <c>results</c> key sits inside the front matter. herald owns that block and rewrites
/// it whole; everything around it belongs to the author and is never touched, which is why this
/// works on positions in the original text instead of on a parsed document.
/// </summary>
internal static class ResultsBlock
{
    private const string Key = "results:";

    /// <summary>
    /// The range the block occupies, or an empty range at the end when the front matter has no
    /// <c>results</c> yet. An empty range is an insertion point, so both cases are written the
    /// same way.
    ///
    /// The range owns the line break in front of the block and not the one behind it. That is what
    /// lets a block be removed without leaving a blank line where it stood, and what keeps a blank
    /// line the author put between the block and the next key.
    /// </summary>
    internal static Range Find(string frontMatter)
    {
        var span = frontMatter.AsSpan();
        var start = -1;
        var end = -1;
        var cursor = 0;

        while (cursor < span.Length)
        {
            var lineBreak = span[cursor..].IndexOf('\n');
            var lineEnd = lineBreak < 0 ? span.Length : cursor + lineBreak;
            var line = span[cursor..lineEnd];

            if (start < 0)
            {
                if (line.StartsWith(Key, StringComparison.Ordinal))
                {
                    start = cursor;
                    end = EndOf(span, lineEnd);
                }
            }
            else if (IsIndented(line))
            {
                // A blank line inside the block is carried along by the next indented one. One that
                // trails the block is not, so it stays with the author's text.
                end = EndOf(span, lineEnd);
            }
            else if (!IsBlank(line))
            {
                break;
            }

            cursor = lineBreak < 0 ? span.Length : lineEnd + 1;
        }

        if (start < 0)
        {
            return span.Length..span.Length;
        }

        var blockStart = start;

        if (blockStart > 0 && span[blockStart - 1] == '\n')
        {
            blockStart--;

            if (blockStart > 0 && span[blockStart - 1] == '\r')
            {
                blockStart--;
            }
        }

        return blockStart..end;
    }

    private static int EndOf(ReadOnlySpan<char> span, int lineEnd) =>
        lineEnd > 0 && span[lineEnd - 1] == '\r' ? lineEnd - 1 : lineEnd;

    private static bool IsBlank(ReadOnlySpan<char> line) => line.TrimEnd('\r').IsWhiteSpace();

    private static bool IsIndented(ReadOnlySpan<char> line) => line.Length > 0 && line[0] is ' ' or '\t';
}
