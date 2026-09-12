namespace Herald.Functions;

internal static partial class FunctionLogger
{
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message = "{FunctionName} started at {StartedAtUtc:O}")]
    internal static partial void Started(this ILogger logger, string functionName, DateTimeOffset startedAtUtc);
}
