namespace Herald.Core.Tokens;

/// <summary>
/// Checks every token herald holds and opens an issue in
/// the content repository for the ones that are about to run out.
/// </summary>
public interface ITokenExpiryCheck
{
    Task<TokenExpiryReport> RunAsync(CancellationToken cancellationToken = default);
}
