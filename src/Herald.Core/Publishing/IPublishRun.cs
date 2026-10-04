namespace Herald.Core.Publishing;

/// <summary>
/// One run over the content repository: read the tree, pick the post files, parse each one. The
/// timer and the on-demand trigger both carry this out, so what happens never depends on who asked.
/// </summary>
public interface IPublishRun
{
    Task<RunReport> RunAsync(CancellationToken cancellationToken = default);
}
