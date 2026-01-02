using Microsoft.Extensions.Logging;
using System.Net;

namespace Kontent.Ai.Delivery.Logging;

/// <summary>
/// High-performance log messages using source generators.
/// </summary>
internal static partial class LoggerMessages
{
    // ========== Query Execution ==========

    [LoggerMessage(
        EventId = LogEventIds.QueryStarting,
        Level = LogLevel.Debug,
        Message = "Starting {QueryType} query for '{Identifier}'")]
    public static partial void QueryStarting(
        ILogger logger,
        string queryType,
        string identifier);

    [LoggerMessage(
        EventId = LogEventIds.QueryCompleted,
        Level = LogLevel.Information,
        Message = "Completed {QueryType} query for '{Identifier}' in {ElapsedMs}ms (Status: {StatusCode}, CacheHit: {CacheHit})")]
    public static partial void QueryCompleted(
        ILogger logger,
        string queryType,
        string identifier,
        long elapsedMs,
        HttpStatusCode statusCode,
        bool cacheHit);

    [LoggerMessage(
        EventId = LogEventIds.QueryFailed,
        Level = LogLevel.Error,
        Message = "Query {QueryType} failed for '{Identifier}' (Status: {StatusCode}, Error: {ErrorMessage})")]
    public static partial void QueryFailed(
        ILogger logger,
        string queryType,
        string identifier,
        HttpStatusCode statusCode,
        string? errorMessage,
        Exception? exception);

    [LoggerMessage(
        EventId = LogEventIds.QueryCacheHit,
        Level = LogLevel.Debug,
        Message = "Cache hit for key '{CacheKey}'")]
    public static partial void QueryCacheHit(ILogger logger, string cacheKey);

    [LoggerMessage(
        EventId = LogEventIds.QueryCacheMiss,
        Level = LogLevel.Debug,
        Message = "Cache miss for key '{CacheKey}', executing API call")]
    public static partial void QueryCacheMiss(ILogger logger, string cacheKey);

    [LoggerMessage(
        EventId = LogEventIds.QueryStaleContent,
        Level = LogLevel.Warning,
        Message = "Response contains stale content for '{Identifier}' (X-Stale-Content header present)")]
    public static partial void QueryStaleContent(ILogger logger, string identifier);

    // ========== Cache Operations ==========

    [LoggerMessage(
        EventId = LogEventIds.CacheGetFailed,
        Level = LogLevel.Warning,
        Message = "Cache read failed for key '{CacheKey}', proceeding with API call")]
    public static partial void CacheGetFailed(ILogger logger, string cacheKey, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.CacheSetCompleted,
        Level = LogLevel.Debug,
        Message = "Cached response for key '{CacheKey}' with {DependencyCount} dependencies")]
    public static partial void CacheSetCompleted(ILogger logger, string cacheKey, int dependencyCount);

    [LoggerMessage(
        EventId = LogEventIds.CacheSetFailed,
        Level = LogLevel.Warning,
        Message = "Cache write failed for key '{CacheKey}', response still returned to caller")]
    public static partial void CacheSetFailed(ILogger logger, string cacheKey, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.CacheInvalidateStarting,
        Level = LogLevel.Debug,
        Message = "Invalidating cache entries for {DependencyCount} dependencies")]
    public static partial void CacheInvalidateStarting(ILogger logger, int dependencyCount);

    [LoggerMessage(
        EventId = LogEventIds.CacheInvalidateCompleted,
        Level = LogLevel.Information,
        Message = "Invalidated cache entries for dependency '{DependencyKey}'")]
    public static partial void CacheInvalidateCompleted(ILogger logger, string dependencyKey);

    [LoggerMessage(
        EventId = LogEventIds.CacheEntryEvicted,
        Level = LogLevel.Debug,
        Message = "Cache entry evicted: '{CacheKey}' (Reason: {EvictionReason})")]
    public static partial void CacheEntryEvicted(ILogger logger, string cacheKey, string evictionReason);

    [LoggerMessage(
        EventId = LogEventIds.CacheSerializationFailed,
        Level = LogLevel.Warning,
        Message = "Failed to serialize value for cache key '{CacheKey}' (Type: {TypeName})")]
    public static partial void CacheSerializationFailed(ILogger logger, string cacheKey, string typeName, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.CacheDeserializationFailed,
        Level = LogLevel.Warning,
        Message = "Failed to deserialize cached value for key '{CacheKey}' (Type: {TypeName})")]
    public static partial void CacheDeserializationFailed(ILogger logger, string cacheKey, string typeName, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.CachePartialItemsWarning,
        Level = LogLevel.Warning,
        Message = "Partial cache: Only {CachedCount} of {TotalCount} items could be cached. Some items may not be the expected concrete type.")]
    public static partial void CachePartialItemsWarning(ILogger logger, int cachedCount, int totalCount);

    // ========== HTTP Handlers ==========

    [LoggerMessage(
        EventId = LogEventIds.HttpAuthSet,
        Level = LogLevel.Trace,
        Message = "Authentication header set (AuthType: {AuthType}, EnvironmentId: {EnvironmentId})")]
    public static partial void HttpAuthSet(ILogger logger, string authType, string environmentId);

    [LoggerMessage(
        EventId = LogEventIds.HttpAuthCleared,
        Level = LogLevel.Trace,
        Message = "Authentication header cleared (no API key configured)")]
    public static partial void HttpAuthCleared(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.HttpEndpointRewritten,
        Level = LogLevel.Debug,
        Message = "Request URI rewritten from '{OriginalHost}' to '{NewHost}'")]
    public static partial void HttpEndpointRewritten(ILogger logger, string originalHost, string newHost);

    [LoggerMessage(
        EventId = LogEventIds.HttpEnvironmentIdInjected,
        Level = LogLevel.Trace,
        Message = "Environment ID '{EnvironmentId}' injected into request path")]
    public static partial void HttpEnvironmentIdInjected(ILogger logger, string environmentId);

    [LoggerMessage(
        EventId = LogEventIds.HttpTrackingHeadersAdded,
        Level = LogLevel.Trace,
        Message = "SDK tracking headers added (SDK: {SdkVersion})")]
    public static partial void HttpTrackingHeadersAdded(ILogger logger, string sdkVersion);

    // ========== Resilience ==========

    [LoggerMessage(
        EventId = LogEventIds.ResilienceRetryAttempt,
        Level = LogLevel.Warning,
        Message = "Retry attempt {AttemptNumber}/{MaxAttempts} for {RequestUri} after {StatusCode} (delay: {DelayMs}ms)")]
    public static partial void ResilienceRetryAttempt(
        ILogger logger,
        int attemptNumber,
        int maxAttempts,
        string requestUri,
        HttpStatusCode statusCode,
        long delayMs);

    [LoggerMessage(
        EventId = LogEventIds.ResilienceRetryExhausted,
        Level = LogLevel.Error,
        Message = "All {MaxAttempts} retry attempts exhausted for {RequestUri} (final status: {StatusCode})")]
    public static partial void ResilienceRetryExhausted(
        ILogger logger,
        int maxAttempts,
        string requestUri,
        HttpStatusCode statusCode);

    [LoggerMessage(
        EventId = LogEventIds.ResilienceTimeout,
        Level = LogLevel.Warning,
        Message = "Request timed out after {TimeoutSeconds}s for {RequestUri}")]
    public static partial void ResilienceTimeout(
        ILogger logger,
        double timeoutSeconds,
        string requestUri);

    // ========== Service Registration ==========

    [LoggerMessage(
        EventId = LogEventIds.ClientRegistered,
        Level = LogLevel.Information,
        Message = "Delivery client '{ClientName}' registered for environment '{EnvironmentId}'")]
    public static partial void ClientRegistered(ILogger logger, string clientName, string environmentId);

    [LoggerMessage(
        EventId = LogEventIds.CacheManagerRegistered,
        Level = LogLevel.Information,
        Message = "Cache manager '{CacheType}' registered for client '{ClientName}' (KeyPrefix: {KeyPrefix})")]
    public static partial void CacheManagerRegistered(ILogger logger, string cacheType, string clientName, string? keyPrefix);
}
