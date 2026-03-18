using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;

/// <summary>
/// Eliminates logging boilerplate across query builders by capturing
/// the two parameters that vary (queryType, identifier).
/// </summary>
internal sealed class QueryLoggingHelper(ILogger? logger, string queryType, string identifier)
{
    public void LogQueryStarting()
    {
        if (logger is not null)
            LoggerMessages.QueryStarting(logger, queryType, identifier);
    }

    public Stopwatch? StartTimingIfEnabled() =>
        logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    public void LogQueryFailed(HttpStatusCode statusCode, string? errorMessage)
    {
        if (logger is not null)
            LoggerMessages.QueryFailed(logger, queryType, identifier, statusCode, errorMessage, exception: null);
    }

    public void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(logger, identifier);
        LoggerMessages.QueryCompleted(logger, queryType, identifier,
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
