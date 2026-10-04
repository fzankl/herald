using System.Diagnostics;
using Herald.Core.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Options;

namespace Herald.Core.Content;

/// <inheritdoc />
public sealed class PostFileSelector : IPostFileSelector
{
    private readonly ContentRepositoryOptions _options;

    public PostFileSelector(IOptions<ContentRepositoryOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public PostFileSelection Select(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        // Ordinal because git paths are case sensitive. The exclude sits in the same matcher as the
        // include, so a template is never read as a post, whatever the pattern looks like.
        var matcher = new Matcher(StringComparison.Ordinal);
        matcher.AddInclude(_options.PostPattern!);
        matcher.AddExclude($"{_options.TemplateFolder}/**");

        var warnings = new List<string>();
        var files = new List<PostFile>();

        // The paths are matched against a root of their own, so that the working directory of the
        // process never takes part in the result.
        var matched = matcher.Match("/", paths).Files.Select(match => match.Path);

        foreach (var group in matched.GroupBy(Path.GetFileNameWithoutExtension, StringComparer.Ordinal))
        {
            var slug = group.Key!;

            if (group.Count() > 1)
            {
                // Which one a series means is not a guess herald makes.
                warnings.Add($"the slug '{slug}' is used by more than one file: {string.Join(", ", group.Select(path => $"'{path}'"))}. None of them is read.");
                continue;
            }

            files.Add(new PostFile
            {
                Slug = slug,
                Path = group.Single()
            });
        }

        return new PostFileSelection(files, warnings);
    }
}
