# Performance Optimization Guide

This guide provides comprehensive strategies for optimizing the performance of applications using the Kontent.ai Delivery SDK, from query optimization to monitoring and diagnostics.

## Table of Contents

- [Overview](#overview)
- [Query Optimization](#query-optimization)
  - [Projection Limiting](#projection-limiting)
  - [Depth Control](#depth-control)
  - [Efficient Filtering](#efficient-filtering)
  - [Pagination Strategies](#pagination-strategies)
- [Caching Strategies](#caching-strategies)
  - [Cache-First Approach](#cache-first-approach)
  - [Cache Warming](#cache-warming)
  - [Stale-While-Revalidate](#stale-while-revalidate)
- [Network Optimization](#network-optimization)
  - [HTTP Client Configuration](#http-client-configuration)
  - [Connection Pooling](#connection-pooling)
  - [Retry Policies](#retry-policies)
- [Parallel Operations](#parallel-operations)
- [Rate Limit Management](#rate-limit-management)
- [Memory Optimization](#memory-optimization)
- [Monitoring and Diagnostics](#monitoring-and-diagnostics)
- [Production Best Practices](#production-best-practices)
- [Performance Benchmarks](#performance-benchmarks)
- [Troubleshooting](#troubleshooting)

## Overview

Performance optimization for Kontent.ai applications involves:

1. **Minimizing API calls** through caching and efficient queries
2. **Reducing payload sizes** with projection and depth control
3. **Optimizing network usage** with proper HTTP client configuration
4. **Monitoring performance** to identify bottlenecks
5. **Managing rate limits** effectively

## Query Optimization

### Projection Limiting

Only retrieve the elements you need using `WithElements()`:

```csharp
// ❌ Bad: Retrieves all elements
var result = await client.GetItems<Article>()
    .Limit(10)
    .ExecuteAsync();

// ✅ Good: Only retrieves needed elements
var result = await client.GetItems<Article>()
    .WithElements("title", "summary", "publish_date", "url_slug")
    .Limit(10)
    .ExecuteAsync();
```

**Impact**: Reducing elements can decrease response size by 50-80% for content-heavy items.

### Depth Control

Limit linked content depth to avoid deep object graphs:

```csharp
// ❌ Bad: Deep nesting (default or high depth)
var result = await client.GetItem<Article>("my-article")
    .Depth(5)  // Too deep
    .ExecuteAsync();

// ✅ Good: Minimal necessary depth
var result = await client.GetItem<Article>("my-article")
    .Depth(1)  // Only first level of linked items
    .ExecuteAsync();

// ✅ Better: No linked items if not needed
var result = await client.GetItem<Article>("my-article")
    .Depth(0)  // No linked content
    .ExecuteAsync();
```

**Impact**: Each depth level can multiply response size. Depth 0 vs Depth 2 can be 10x size difference.

### Combining Projection and Depth

```csharp
// Optimal query: minimal depth + only needed elements
var result = await client.GetItems<Article>()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    .WithElements("title", "summary", "featured_image")
    .Depth(0)  // No linked content
    .Limit(20)
    .ExecuteAsync();
```

### Efficient Filtering

Use indexed system properties when possible:

```csharp
.Filter(f => f.Equals(ItemSystemPath.Type, "article"))
.Filter(f => f.Equals(ItemSystemPath.Collection, "blog"))
.Filter(f => f.GreaterThan(ItemSystemPath.LastModified, cutoffDate))
.Filter(f => f.Equals(Elements.GetPath("category"), "tech"))
```

### Pagination Strategies

#### Standard Pagination

For user-facing pagination:

```csharp
public async Task<PagedResult<Article>> GetArticlesAsync(int page, int pageSize)
{
    var result = await client.GetItems<Article>()
        .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
        .OrderBy(ItemSystemPath.LastModified, descending: true)
        .Skip(page * pageSize)
        .Limit(pageSize)
        .WithTotalCount()
        .ExecuteAsync();

    return new PagedResult<Article>
    {
        Items = result.Value.ToList(),
        TotalCount = result.Value.TotalCount ?? 0,
        Page = page,
        PageSize = pageSize
    };
}
```

#### Items Feed for Bulk Operations

For processing all items efficiently:

```csharp
// ✅ Best for bulk: Automatic pagination with continuation tokens
var query = client.GetItemsFeed<Article>()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    .WithElements("title", "url_slug")  // Only needed elements
    .OrderBy(ItemSystemPath.Codename, ascending: true);

await foreach (var article in query.ExecuteAsync())
{
    // Process each article
    await ProcessArticleAsync(article);
}
```

**Impact**: Items feed is 2-3x faster than manual pagination for bulk operations.

## Caching Strategies

### Cache-First Approach

Always configure caching in production:

```csharp
// Development: Short cache
services.AddDeliveryClient(options => { ... })
    .WithMemoryCache(TimeSpan.FromMinutes(5));

// Production: Longer cache with distributed storage
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379";
});

services.AddDeliveryClient(options => { ... })
    .WithDistributedCache(TimeSpan.FromHours(4));
```

### Cache Warming

Pre-populate cache on application startup:

```csharp
public class CacheWarmupService : IHostedService
{
    private readonly IDeliveryClient _client;
    private readonly ILogger<CacheWarmupService> _logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Warm critical pages
            await WarmCriticalContentAsync(cancellationToken);

            _logger.LogInformation(
                "Cache warmed in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warmup failed");
        }
    }

    private async Task WarmCriticalContentAsync(CancellationToken cancellationToken)
    {
        var criticalPages = new[] { "homepage", "navigation", "footer", "sitemap" };

        var tasks = criticalPages.Select(codename =>
            client.GetItem(codename).ExecuteAsync(cancellationToken));

        await Task.WhenAll(tasks);

        // Warm recent articles
        await client.GetItems<Article>()
            .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
            .OrderBy(ItemSystemPath.LastModified, descending: true)
            .Limit(20)
            .ExecuteAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// Register
services.AddHostedService<CacheWarmupService>();
```

### Stale-While-Revalidate

Serve stale content while refreshing in background:

```csharp
public class StaleWhileRevalidateService
{
    private readonly IDeliveryClient _client;
    private readonly IMemoryCache _cache;

    public async Task<T?> GetWithStaleAsync<T>(
        string cacheKey,
        Func<Task<T>> fetchFunc,
        TimeSpan freshDuration,
        TimeSpan staleDuration)
    {
        if (_cache.TryGetValue(cacheKey, out CachedItem<T> cached))
        {
            // If fresh, return immediately
            if (DateTime.UtcNow - cached.Timestamp < freshDuration)
                return cached.Value;

            // If stale but within stale duration, return and refresh in background
            if (DateTime.UtcNow - cached.Timestamp < staleDuration)
            {
                _ = Task.Run(async () =>
                {
                    var fresh = await fetchFunc();
                    _cache.Set(cacheKey, new CachedItem<T>
                    {
                        Value = fresh,
                        Timestamp = DateTime.UtcNow
                    });
                });

                return cached.Value;  // Return stale
            }
        }

        // No cache or too stale, fetch fresh
        var value = await fetchFunc();
        _cache.Set(cacheKey, new CachedItem<T>
        {
            Value = value,
            Timestamp = DateTime.UtcNow
        });

        return value;
    }
}

public class CachedItem<T>
{
    public T Value { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## Network Optimization

### HTTP Client Configuration

Configure HTTP client for optimal performance:

```csharp
services.AddDeliveryClient(
    options =>
    {
        options.EnvironmentId = "your-environment-id";
    },
    configureHttpClient: builder =>
    {
        builder.ConfigureHttpClient(client =>
        {
            // Timeout
            client.Timeout = TimeSpan.FromSeconds(30);

            // Headers
            client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
        });
    });
```

### Connection Pooling

Use HTTP client factory for proper connection pooling (handled automatically by SDK):

```csharp
// SDK handles this automatically through HttpClientFactory
// No manual configuration needed - just benefits you get for free!
```

**Impact**: Connection pooling prevents port exhaustion and reduces latency by 20-30%.

### Retry Policies

Configure resilience policies for transient failures:

```csharp
services.AddDeliveryClient(
    options => { ... },
    configureResilience: builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        });

        builder.AddTimeout(TimeSpan.FromSeconds(30));
    });
```

## Parallel Operations

### Parallel Queries

Execute independent queries in parallel:

```csharp
public async Task<DashboardData> GetDashboardDataAsync()
{
    // Execute queries in parallel
    var homepageTask = client.GetItem<HomePage>("homepage").ExecuteAsync();
    var articlesTask = client.GetItems<Article>()
        .OrderBy(ItemSystemPath.LastModified, descending: true)
        .Limit(5)
        .ExecuteAsync();
    var productsTask = client.GetItems<Product>()
        .Filter(f => f.Any(Elements.GetPath("tags"), "featured"))
        .Limit(10)
        .ExecuteAsync();

    // Wait for all
    await Task.WhenAll(homepageTask, articlesTask, productsTask);

    return new DashboardData
    {
        Homepage = homepageTask.Result.Value,
        RecentArticles = articlesTask.Result.Value.ToList(),
        FeaturedProducts = productsTask.Result.Value.ToList()
    };
}
```

**Impact**: 3 parallel queries complete in ~1 second vs. 3 seconds sequentially.

### Batching Content Retrieval

Retrieve multiple items efficiently:

```csharp
// ✅ Good: Single query with filter
var codenamesList = new[] { "article1", "article2", "article3" };
var result = await client.GetItems<Article>()
    .Filter(f => f.In(ItemSystemPath.Codename, codenamesList))
    .ExecuteAsync();

// ❌ Bad: Multiple queries
foreach (var codename in codenamesList)
{
    await client.GetItem<Article>(codename).ExecuteAsync();
}
```

## Rate Limit Management

### Understanding Rate Limits

Kontent.ai enforces rate limits:
- Requests per second
- Burst capacity
- Monthly quota

### Monitoring Rate Limits

Track API usage:

```csharp
public class RateLimitMonitor
{
    private long _requestCount;
    private readonly ILogger _logger;

    public void RecordRequest()
    {
        var count = Interlocked.Increment(ref _requestCount);

        if (count % 100 == 0)
        {
            _logger.LogInformation("Total API requests: {Count}", count);
        }
    }

    public long GetRequestCount() => Interlocked.Read(ref _requestCount);
}
```

### Rate Limit Response Handling

The SDK's retry policy handles 429 responses automatically:

```csharp
services.AddDeliveryClient(
    options => { ... },
    configureResilience: builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential
        });
    });
```

### Rate Limit Mitigation

1. **Cache aggressively**: Primary defense against rate limits
2. **Use items feed**: More efficient for bulk operations
3. **Batch requests**: Retrieve multiple items in single queries
4. **Monitor usage**: Track request patterns and optimize

## Memory Optimization

### Limit Cache Size

Configure memory cache limits:

```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;  // Maximum number of entries
    options.CompactionPercentage = 0.25;  // Remove 25% when limit hit
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});
```

### Use Projection

Reduce memory footprint by limiting elements:

```csharp
// ❌ Large memory footprint: Full content with all elements
var items = await client.GetItems<Article>()
    .Limit(100)
    .ExecuteAsync();

// ✅ Smaller footprint: Only needed elements
var items = await client.GetItems<Article>()
    .WithElements("title", "url_slug", "publish_date")
    .Limit(100)
    .ExecuteAsync();
```

### Dispose Resources

Ensure proper cleanup (SDK handles this automatically via DI):

```csharp
// ✅ Good: Using DI (automatic disposal)
public class MyService
{
    private readonly IDeliveryClient _client;

    public MyService(IDeliveryClient client)
    {
        _client = client;  // Managed by DI container
    }
}

// ❌ Bad: Manual instantiation (potential leak)
var client = new DeliveryClient(...);  // Don't do this
```

## Monitoring and Diagnostics

### Application Insights Integration

```csharp
public class TelemetryClientWrapper
{
    private readonly IDeliveryClient _client;
    private readonly TelemetryClient _telemetry;

    public async Task<IDeliveryResult<T>> GetItemWithTelemetryAsync<T>(string codename)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _client.GetItem<T>(codename).ExecuteAsync();

            stopwatch.Stop();

            _telemetry.TrackDependency(
                "Kontent.ai",
                "GetItem",
                codename,
                DateTimeOffset.UtcNow,
                stopwatch.Elapsed,
                result.IsSuccess);

            _telemetry.TrackMetric(
                "KontentApi.Duration",
                stopwatch.ElapsedMilliseconds,
                new Dictionary<string, string>
                {
                    ["Operation"] = "GetItem",
                    ["Codename"] = codename,
                    ["Success"] = result.IsSuccess.ToString()
                });

            return result;
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
```

### Performance Logging

```csharp
public class PerformanceLoggingClient : IDeliveryClient
{
    private readonly IDeliveryClient _inner;
    private readonly ILogger _logger;

    public async Task<IDeliveryResult<IContentItem>> GetItemAsync(string codename)
    {
        var sw = Stopwatch.StartNew();

        var result = await _inner.GetItem(codename).ExecuteAsync();

        sw.Stop();

        _logger.LogInformation(
            "GetItem({Codename}) completed in {ElapsedMs}ms - Success: {Success}",
            codename,
            sw.ElapsedMilliseconds,
            result.IsSuccess);

        if (sw.ElapsedMilliseconds > 1000)
        {
            _logger.LogWarning(
                "Slow query detected: GetItem({Codename}) took {ElapsedMs}ms",
                codename,
                sw.ElapsedMilliseconds);
        }

        return result;
    }
}
```

### Cache Metrics

```csharp
public class CacheMetricsCollector
{
    private long _hits;
    private long _misses;
    private long _totalDuration;

    public void RecordHit(long durationMs)
    {
        Interlocked.Increment(ref _hits);
        Interlocked.Add(ref _totalDuration, durationMs);
    }

    public void RecordMiss(long durationMs)
    {
        Interlocked.Increment(ref _misses);
        Interlocked.Add(ref _totalDuration, durationMs);
    }

    public CacheStatistics GetStatistics()
    {
        var hits = Interlocked.Read(ref _hits);
        var misses = Interlocked.Read(ref _misses);
        var total = hits + misses;

        return new CacheStatistics
        {
            Hits = hits,
            Misses = misses,
            HitRate = total > 0 ? (double)hits / total : 0,
            AverageDuration = total > 0
                ? Interlocked.Read(ref _totalDuration) / (double)total
                : 0
        };
    }
}

public class CacheStatistics
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public double HitRate { get; set; }
    public double AverageDuration { get; set; }
}
```

## Production Best Practices

### 1. Always Use Caching

```csharp
// ✅ Production configuration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
    options.InstanceName = "Production_";
});

services.AddDeliveryClient(options =>
{
    options.EnvironmentId = configuration["Kontent:EnvironmentId"];
    options.EnableResilience = true;
})
.WithDistributedCache(TimeSpan.FromHours(4));
```

### 2. Configure Retry Policies

```csharp
configureResilience: builder =>
{
    builder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    });
}
```

### 3. Implement Health Checks

```csharp
public class KontentHealthCheck : IHealthCheck
{
    private readonly IDeliveryClient _client;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var result = await _client.GetItem("health-check-item")
                .ExecuteAsync(cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                return HealthCheckResult.Healthy(
                    $"Kontent.ai API responsive ({stopwatch.ElapsedMilliseconds}ms)");
            }

            return HealthCheckResult.Degraded("Failed to retrieve content");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Kontent.ai API unavailable", ex);
        }
    }
}

// Register
services.AddHealthChecks()
    .AddCheck<KontentHealthCheck>("kontent");
```

### 4. Monitor and Alert

Set up monitoring for:
- API response times
- Cache hit rates
- Error rates
- Rate limit proximity
- Memory usage

### 5. Use CDN for Assets

Serve images and assets through CDN:

```csharp
public class AssetUrlService
{
    private readonly string _cdnUrl;

    public string GetAssetUrl(string assetUrl)
    {
        // Use CDN for assets
        return assetUrl.Replace("assets-us-01.kc-usercontent.com", "cdn.yoursite.com");
    }
}
```

## Performance Benchmarks

### Typical Response Times

| Operation | No Cache | With Cache | Improvement |
|-----------|----------|------------|-------------|
| Get Single Item | 150-300ms | 1-5ms | 50-300x |
| Get 10 Items | 200-400ms | 2-10ms | 40-200x |
| Get Items Feed (100 items) | 500-1000ms | 5-20ms | 50-200x |
| Rich Text Resolution | 50-100ms | <1ms | 50-100x |

### Cache Hit Rate Targets

- **Good**: 80%+ cache hit rate
- **Excellent**: 90%+ cache hit rate
- **Outstanding**: 95%+ cache hit rate

## Troubleshooting

### Slow Queries

**Problem**: Queries take several seconds.

**Solutions**:

1. **Enable caching**
2. **Reduce depth**: `Depth(0)` or `Depth(1)`
3. **Limit elements**: Use `WithElements()`
4. **Optimize filters**: Use system properties
5. **Check network**: Verify connectivity and latency

### High Memory Usage

**Problem**: Application using too much memory.

**Solutions**:

1. **Configure cache limits**:
```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 512;
});
```

2. **Use distributed cache** instead of memory cache
3. **Limit depth** and elements in queries
4. **Monitor for memory leaks**

### Rate Limit Errors

**Problem**: Receiving 429 (Too Many Requests) errors.

**Solutions**:

1. **Implement caching** (primary solution)
2. **Reduce API calls** through batching
3. **Use items feed** for bulk operations
4. **Add retry policies** with backoff
5. **Contact Kontent.ai** to increase limits if needed

### Cache Misses

**Problem**: Low cache hit rate.

**Solutions**:

1. **Increase cache expiration** time
2. **Warm cache** on startup
3. **Verify cache is configured** correctly
4. **Check query consistency** (different parameters = different cache keys)

---

**Related Documentation**:
- [Main README](../README.md)
- [Caching Guide](caching-guide.md)
- [Advanced Filtering](advanced-filtering.md)
- [Multi-Client Scenarios](multi-client-scenarios.md)
