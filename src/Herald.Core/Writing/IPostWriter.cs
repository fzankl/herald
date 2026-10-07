using Herald.Core.Models;

namespace Herald.Core.Writing;

/// <summary>
/// Puts a post file back together with a new <c>results</c> block. Everything else stays byte for
/// byte as the author left it, key order, quoting and comments included.
/// </summary>
internal interface IPostWriter
{
    WriteResult Write(string content, IReadOnlyDictionary<string, TargetResult> results);
}
