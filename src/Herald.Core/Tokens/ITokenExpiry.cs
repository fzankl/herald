namespace Herald.Core.Tokens;

/// <summary>
/// One access token herald holds, and when it runs out. Every target that needs a token brings its
/// own implementation, so that checking them all is a matter of registering one more.
/// </summary>
internal interface ITokenExpiry
{
    /// <summary>
    /// What to call the token in a warning. It reaches a human,
    /// so it names the thing the token opens and not the setting it is stored in.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// When the token expires, or null when the service does not say. Null is not reassurance: it
    /// means herald cannot tell, and a warning that never comes looks exactly the same.
    /// </summary>
    Task<DateTimeOffset?> GetExpiryAsync(CancellationToken cancellationToken = default);
}
