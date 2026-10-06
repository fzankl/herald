namespace Herald.Core.Tokens;

/// <summary>
/// What one check of the tokens found, one entry per token.
/// </summary>
public sealed record TokenExpiryReport
{
    public required IReadOnlyList<TokenExpiryEntry> Tokens { get; init; }

    /// <summary>
    /// How many tokens need attention.
    /// </summary>
    public int Warned => Tokens.Count(token => token.State is TokenState.Expiring or TokenState.Expired);
}
