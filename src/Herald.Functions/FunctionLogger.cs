using Herald.Core.Tokens;

namespace Herald.Functions;

internal static partial class FunctionLogger
{
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message = "{FunctionName} started at {StartedAtUtc:O}")]
    internal static partial void Started(this ILogger logger, string functionName, DateTimeOffset startedAtUtc);

    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Information,
        Message = "Run finished: {FilesInRepository} files, {Posts} posts, {Due} due, {Overdue} overdue, {Rejected} rejected, {Warnings} warnings")]
    internal static partial void RunFinished(this ILogger logger, int filesInRepository, int posts, int due, int overdue, int rejected, int warnings);

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Warning,
        Message = "Post '{Slug}' was rejected: {Reasons}")]
    internal static partial void PostRejected(this ILogger logger, string slug, string reasons);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Warning,
        Message = "{Warning}")]
    internal static partial void SelectionWarning(this ILogger logger, string warning);

    [LoggerMessage(
        EventId = 300,
        Level = LogLevel.Information,
        Message = "Tokens checked: {Tokens} tokens, {Warned} need attention")]
    internal static partial void TokensChecked(this ILogger logger, int tokens, int warned);

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Warning,
        Message = "The {Name} is {State}, expires at {ExpiresAt:O}, issue opened: {IssueOpened}")]
    internal static partial void TokenNeedsAttention(this ILogger logger, string name, TokenState state, DateTimeOffset? expiresAt, bool issueOpened);
}
