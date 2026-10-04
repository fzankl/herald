using Herald.Core.Models;

namespace Herald.Core.Parsing;

/// <summary>
/// Reads one post file into a <see cref="Post"/>, or into the reasons why it is rejected. It knows
/// nothing about git, HTTP or the clock, so every rule here can be tested with a string.
/// </summary>
internal interface IPostParser
{
    /// <summary>
    /// Reads <paramref name="content"/>, the whole file including its front matter.
    /// </summary>
    /// <param name="slug">The file name without <c>.md</c>, which becomes the post's slug.</param>
    /// <param name="content">The file as it stands in the content repository.</param>
    /// <returns>A result carrying the parsed <see cref="Post"/>, or the reasons the file was rejected.</returns>
    PostParseResult Parse(string slug, string content);
}
