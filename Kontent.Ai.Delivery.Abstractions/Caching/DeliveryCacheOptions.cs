namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Configuration options for the Delivery SDK cache layer.
/// </summary>
/// <remarks>
/// <para>
/// All defaults match the existing SDK behavior (no fail-safe, no jitter).
/// </para>
/// <para>
/// <b>Fail-safe</b> allows the cache to serve stale (expired) entries when the underlying
/// data source is unavailable, providing resilience during API outages.
/// </para>
/// <para>
/// <b>Jitter</b> randomizes the expiration time of cache entries to prevent the
/// "thundering herd" problem where many entries expire simultaneously.
/// </para>
/// </remarks>
public sealed class DeliveryCacheOptions
{
    /// <summary>
    /// Gets or sets the default expiration for cache entries.
    /// Individual queries can override this value.
    /// </summary>
    /// <value>Defaults to 1 hour.</value>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets an optional prefix for all cache keys.
    /// Used to isolate cache entries when multiple clients share the same cache store.
    /// </summary>
    /// <value>Defaults to <see langword="null"/> (no prefix).</value>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether fail-safe mode is enabled.
    /// When enabled, the cache returns stale (expired) entries when the data source
    /// is unavailable, providing resilience during API outages.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    public bool IsFailSafeEnabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum duration a stale cache entry can be served
    /// when fail-safe is enabled. After this period, the entry is permanently removed.
    /// </summary>
    /// <value>Defaults to 1 day.</value>
    /// <remarks>Only applies when <see cref="IsFailSafeEnabled"/> is <see langword="true"/>.</remarks>
    public TimeSpan FailSafeMaxDuration { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Gets or sets the minimum duration between attempts to refresh a stale entry
    /// when fail-safe is active. This prevents excessive refresh attempts during outages.
    /// </summary>
    /// <value>Defaults to 30 seconds.</value>
    /// <remarks>Only applies when <see cref="IsFailSafeEnabled"/> is <see langword="true"/>.</remarks>
    public TimeSpan FailSafeThrottleDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum random duration added to entry expiration times
    /// to spread out synchronized expirations and prevent the "thundering herd" problem.
    /// </summary>
    /// <value>Defaults to <see cref="TimeSpan.Zero"/> (no jitter).</value>
    public TimeSpan JitterMaxDuration { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the eager refresh threshold as a fraction of the entry's TTL.
    /// When set to a value greater than 0, the cache will proactively refresh entries
    /// in the background before they expire.
    /// </summary>
    /// <value>
    /// A value between 0.0 and 1.0. Defaults to 0.0 (disabled).
    /// For example, 0.8 means the entry will be refreshed after 80% of its TTL has elapsed.
    /// </value>
    public float EagerRefreshThreshold { get; set; }
}
