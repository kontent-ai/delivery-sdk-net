namespace Kontent.Ai.Delivery.Logging;

/// <summary>
/// Centralized Event ID constants for structured logging.
/// Each range is reserved for a specific area of the SDK.
/// </summary>
internal static class LogEventIds
{
    // ========== Query Execution (1000-1099) ==========

    /// <summary>Query execution started.</summary>
    public const int QueryStarting = 1000;

    /// <summary>Query execution completed successfully.</summary>
    public const int QueryCompleted = 1001;

    /// <summary>Query execution failed.</summary>
    public const int QueryFailed = 1002;

    /// <summary>Cache hit - result returned from cache.</summary>
    public const int QueryCacheHit = 1010;

    /// <summary>Cache miss - proceeding with API call.</summary>
    public const int QueryCacheMiss = 1011;

    /// <summary>Dependency tracked during post-processing.</summary>
    public const int QueryDependencyTracked = 1030;

    /// <summary>Response contains stale content.</summary>
    public const int QueryStaleContent = 1040;

    // ========== Cache Operations (1100-1199) ==========

    /// <summary>Cache read operation failed.</summary>
    public const int CacheGetFailed = 1100;

    /// <summary>Cache write operation completed.</summary>
    public const int CacheSetCompleted = 1110;

    /// <summary>Cache write operation failed.</summary>
    public const int CacheSetFailed = 1111;

    /// <summary>Cache invalidation started.</summary>
    public const int CacheInvalidateStarting = 1120;

    /// <summary>Cache invalidation completed.</summary>
    public const int CacheInvalidateCompleted = 1121;

    /// <summary>Cache entry was evicted.</summary>
    public const int CacheEntryEvicted = 1130;

    /// <summary>Serialization failed during cache operation.</summary>
    public const int CacheSerializationFailed = 1140;

    /// <summary>Deserialization failed during cache operation.</summary>
    public const int CacheDeserializationFailed = 1141;

    // ========== HTTP Handlers (1200-1299) ==========

    /// <summary>Authentication header was set on request.</summary>
    public const int HttpAuthSet = 1200;

    /// <summary>Authentication header was cleared (no API key configured).</summary>
    public const int HttpAuthCleared = 1201;

    /// <summary>Request URI was rewritten for endpoint switching.</summary>
    public const int HttpEndpointRewritten = 1210;

    /// <summary>Environment ID was injected into request path.</summary>
    public const int HttpEnvironmentIdInjected = 1211;

    /// <summary>SDK tracking headers were added to request.</summary>
    public const int HttpTrackingHeadersAdded = 1220;

    // ========== Resilience (1300-1399) ==========

    /// <summary>Retry attempt after transient failure.</summary>
    public const int ResilienceRetryAttempt = 1300;

    /// <summary>All retry attempts exhausted.</summary>
    public const int ResilienceRetryExhausted = 1301;

    /// <summary>Request timed out.</summary>
    public const int ResilienceTimeout = 1310;

    // ========== Service Registration (1500-1599) ==========

    /// <summary>Delivery client was registered.</summary>
    public const int ClientRegistered = 1500;

    /// <summary>Cache manager was registered.</summary>
    public const int CacheManagerRegistered = 1510;
}
