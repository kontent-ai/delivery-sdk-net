# Caching Guide

Caching is essential for production applications using the Kontent.ai Delivery API. This guide covers all aspects of implementing effective caching strategies, from basic memory caching to sophisticated webhook-based invalidation.

## Table of Contents

- [Why Caching Matters](#why-caching-matters)
- [Cache Types](#cache-types)
  - [Memory Cache](#memory-cache)
  - [Distributed Cache](#distributed-cache)
- [Configuration](#configuration)
  - [Memory Cache Setup](#memory-cache-setup)
  - [Distributed Cache Setup](#distributed-cache-setup)
  - [Custom Cache Manager](#custom-cache-manager)
- [How Caching Works](#how-caching-works)
  - [Cache Keys](#cache-keys)
  - [Dependency Tracking](#dependency-tracking)
  - [Expiration Strategies](#expiration-strategies)
- [Cache Invalidation](#cache-invalidation)
  - [Manual Invalidation](#manual-invalidation)
  - [Webhook-Based Invalidation](#webhook-based-invalidation)
  - [Timed Invalidation](#timed-invalidation)
- [Multi-Tenant Caching](#multi-tenant-caching)
- [Best Practices](#best-practices)
- [Monitoring and Diagnostics](#monitoring-and-diagnostics)
- [Troubleshooting](#troubleshooting)

## Why Caching Matters

### API Rate Limits

Kontent.ai enforces rate limits on API requests:
- Without caching, you can quickly hit these limits
- Repeated requests for the same content waste quota
- Caching dramatically reduces API calls

### Performance

- **Reduced Latency**: Cached responses are served in microseconds vs. milliseconds for API calls
- **Lower Bandwidth**: No network round-trip for cached content
- **Better User Experience**: Faster page loads and responses

### Cost Optimization

- Fewer API calls mean lower costs in high-traffic scenarios
- Reduced infrastructure requirements for handling API responses
- Better resource utilization

## Cache Types

### Memory Cache

**Pros:**
- Fastest possible cache access (microseconds)
- No external dependencies
- Simple setup

**Cons:**
- Limited to single server (not shared across instances)
- Memory pressure on large datasets
- Lost on application restart

**When to Use:**
- Single-server deployments
- Development and testing
- Low to moderate traffic applications

### Distributed Cache

**Pros:**
- Shared across multiple application instances
- Survives application restarts
- Scalable to large datasets
- Can be managed independently

**Cons:**
- Network latency (still faster than API calls)
- Requires external infrastructure (Redis, SQL Server, etc.)
- Additional configuration complexity

**When to Use:**
- Production environments with multiple servers
- High-availability requirements
- Large-scale applications
- Cloud deployments

## Configuration

### Memory Cache Setup

#### Basic Configuration

```csharp
using Kontent.Ai.Delivery;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
})
.WithMemoryCache(defaultExpiration: TimeSpan.FromHours(1));

var serviceProvider = services.BuildServiceProvider();
```

#### Custom Expiration

```csharp
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
})
.WithMemoryCache(defaultExpiration: TimeSpan.FromMinutes(30));
```

#### Advanced Memory Cache Configuration

```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;  // Limit cache size
    options.CompactionPercentage = 0.25;  // Remove 25% when limit hit
});

services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
})
.WithMemoryCache(defaultExpiration: TimeSpan.FromHours(1));
```

### Distributed Cache Setup

#### Redis Cache

```csharp
using StackExchange.Redis;

// Register Redis distributed cache
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "KontentCache_";
});

// Add delivery client with distributed caching
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
})
.WithDistributedCache(defaultExpiration: TimeSpan.FromHours(2));
```

#### Redis with Connection Multiplexer

```csharp
// Register Redis connection
services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse("localhost:6379");
    configuration.AbortOnConnectFail = false;
    configuration.ConnectTimeout = 5000;
    return ConnectionMultiplexer.Connect(configuration);
});

services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = async () =>
        await Task.FromResult(serviceProvider.GetRequiredService<IConnectionMultiplexer>());
    options.InstanceName = "Kontent_";
});

services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
})
.WithDistributedCache(defaultExpiration: TimeSpan.FromHours(4));
```

#### SQL Server Distributed Cache

```csharp
services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = configuration.GetConnectionString("CacheDb");
    options.SchemaName = "dbo";
    options.TableName = "KontentCache";
});

services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
})
.WithDistributedCache(defaultExpiration: TimeSpan.FromHours(1));
```

#### Azure Cache for Redis

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("AzureRedis");
    options.InstanceName = "Production_Kontent_";
});

services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
})
.WithDistributedCache(defaultExpiration: TimeSpan.FromHours(6));
```

### Custom Cache Manager

For advanced scenarios, implement a custom cache manager:

```csharp
using Kontent.Ai.Delivery.Abstractions;

public class CustomCacheManager : IDeliveryCacheManager
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CustomCacheManager> _logger;

    public CustomCacheManager(IDistributedCache cache, ILogger<CustomCacheManager> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<T?> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (cached == null) return default;

            return JsonSerializer.Deserialize<T>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache retrieval failed for key: {Key}", key);
            return default;
        }
    }

    public async ValueTask SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            var serialized = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serialized, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache set failed for key: {Key}", key);
        }
    }

    public async ValueTask InvalidateAsync(
        CancellationToken cancellationToken = default,
        params string[] dependencies)
    {
        foreach (var dependency in dependencies)
        {
            await _cache.RemoveAsync(dependency, cancellationToken);
        }
    }
}

// Registration
services.AddSingleton<IDeliveryCacheManager, CustomCacheManager>();
```

## How Caching Works

### Cache Keys

Cache keys are automatically generated from query parameters:

```csharp
// Different queries = different cache keys
await client.GetItem("homepage").ExecuteAsync();
// Key: "item_homepage_en-US_0"

await client.GetItem("homepage").WithLanguage("de-DE").ExecuteAsync();
// Key: "item_homepage_de-DE_0"

await client.GetItems().Limit(10).ExecuteAsync();
// Key: "items_{hash-of-filters}_en-US_0_10"

await client.GetItems().Filter(f => f.Equals(ItemSystemPath.Type, "article")).ExecuteAsync();
// Key: "items_{hash-with-type-filter}_en-US_0"
```

Cache keys include:
- Query type (item, items, taxonomy, etc.)
- Filters and parameters
- Language
- Depth
- Pagination (skip/limit)

### Dependency Tracking

The SDK automatically tracks content dependencies:

```csharp
// When you retrieve an article with linked authors
var result = await client.GetItem<Article>("my-article")
    .Depth(2)
    .ExecuteAsync();

// The following dependencies are tracked:
// - item_my-article
// - item_author1 (if linked)
// - item_author2 (if linked)
// - Any assets used in the content
```

This enables targeted cache invalidation when specific content changes.

### Expiration Strategies

#### Absolute Expiration

Cache entries expire after a fixed duration:

```csharp
services.AddDeliveryClient(options => { ... })
    .WithMemoryCache(defaultExpiration: TimeSpan.FromHours(2));
```

#### Sliding Expiration

For custom cache managers, you can implement sliding expiration:

```csharp
public async ValueTask SetAsync<T>(string key, T value, TimeSpan expiration, ...)
{
    var options = new DistributedCacheEntryOptions
    {
        SlidingExpiration = expiration  // Renewed on each access
    };

    await _cache.SetAsync(key, serialized, options, cancellationToken);
}
```

## Cache Invalidation

### Manual Invalidation

Invalidate specific content:

```csharp
var cacheManager = serviceProvider.GetRequiredService<IDeliveryCacheManager>();

// Invalidate a specific item
await cacheManager.InvalidateAsync(default, "item_homepage");

// Invalidate multiple items
await cacheManager.InvalidateAsync(default,
    "item_article1",
    "item_article2",
    "taxonomy_categories");

// Invalidate by dependency
await cacheManager.InvalidateAsync(default, $"item_{articleCodename}");
```

### Webhook-Based Invalidation

Implement automatic cache invalidation using Kontent.ai webhooks:

#### 1. Webhook Controller

```csharp
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IDeliveryCacheManager _cacheManager;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IDeliveryCacheManager cacheManager,
        ILogger<WebhookController> logger)
    {
        _cacheManager = cacheManager;
        _logger = logger;
    }

    [HttpPost("kontent")]
    public async Task<IActionResult> HandleWebhook([FromBody] WebhookNotification notification)
    {
        // Verify webhook signature (recommended)
        if (!VerifySignature(Request.Headers["X-KC-Signature"]))
        {
            return Unauthorized();
        }

        try
        {
            await ProcessWebhookAsync(notification);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing failed");
            return StatusCode(500);
        }
    }

    private async Task ProcessWebhookAsync(WebhookNotification notification)
    {
        var dependencies = new List<string>();

        foreach (var item in notification.Data.Items)
        {
            // Add item dependency
            dependencies.Add($"item_{item.Codename}");

            // If this was a taxonomy change, invalidate taxonomy cache
            if (item.Type == "taxonomy")
            {
                dependencies.Add($"taxonomy_{item.Codename}");
            }
        }

        // Invalidate all affected cache entries
        await _cacheManager.InvalidateAsync(default, dependencies.ToArray());

        _logger.LogInformation(
            "Invalidated {Count} cache entries from webhook",
            dependencies.Count);
    }

    private bool VerifySignature(string signature)
    {
        // Implement webhook signature verification
        // See: https://kontent.ai/learn/docs/webhooks/validate-webhooks
        return true;
    }
}

public class WebhookNotification
{
    public WebhookData Data { get; set; }
    public WebhookMessage Message { get; set; }
}

public class WebhookData
{
    public List<WebhookItem> Items { get; set; }
}

public class WebhookItem
{
    public string Id { get; set; }
    public string Codename { get; set; }
    public string Type { get; set; }
}

public class WebhookMessage
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Operation { get; set; }
}
```

#### 2. Webhook Signature Verification

```csharp
using System.Security.Cryptography;
using System.Text;

private bool VerifyWebhookSignature(string signature, string requestBody, string secret)
{
    if (string.IsNullOrEmpty(signature))
        return false;

    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
    var computedSignature = Convert.ToBase64String(hash);

    return signature == computedSignature;
}

[HttpPost("kontent")]
public async Task<IActionResult> HandleWebhook()
{
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync();

    var signature = Request.Headers["X-KC-Signature"].FirstOrDefault();
    var secret = _configuration["Kontent:WebhookSecret"];

    if (!VerifyWebhookSignature(signature, body, secret))
    {
        _logger.LogWarning("Invalid webhook signature");
        return Unauthorized();
    }

    var notification = JsonSerializer.Deserialize<WebhookNotification>(body);
    await ProcessWebhookAsync(notification);

    return Ok();
}
```

#### 3. Configure Webhook in Kontent.ai

1. Go to **Environment Settings** > **Webhooks**
2. Create a new webhook
3. Set URL to: `https://yourapp.com/api/webhooks/kontent`
4. Select events: "Publish", "Unpublish", "Archive"
5. Save the webhook secret for signature verification

### Timed Invalidation

For content that changes on a schedule:

```csharp
public class CacheInvalidationService : BackgroundService
{
    private readonly IDeliveryCacheManager _cacheManager;
    private readonly ILogger<CacheInvalidationService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Invalidate news cache every 5 minutes
                await _cacheManager.InvalidateAsync(stoppingToken, "items_news");

                // Wait 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache invalidation failed");
            }
        }
    }
}

// Register service
services.AddHostedService<CacheInvalidationService>();
```

## Multi-Tenant Caching

When serving multiple environments or brands:

### Named Clients with Separate Caches

```csharp
services.AddDeliveryClient("brand-a", options =>
{
    options.EnvironmentId = "brand-a-environment-id";
})
.WithMemoryCache(defaultExpiration: TimeSpan.FromHours(1));

services.AddDeliveryClient("brand-b", options =>
{
    options.EnvironmentId = "brand-b-environment-id";
})
.WithMemoryCache(defaultExpiration: TimeSpan.FromHours(1));
```

Cache keys automatically include the environment ID, preventing conflicts.

### Cache Key Prefixing

For distributed caches, use instance names:

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = $"Tenant_{tenantId}_";
});
```

### Per-Tenant Cache Invalidation

```csharp
public class TenantCacheManager
{
    private readonly IDeliveryClientFactory _clientFactory;

    public async Task InvalidateTenantCacheAsync(string tenantId, params string[] dependencies)
    {
        var client = _clientFactory.Get(tenantId);
        var cacheManager = /* get cache manager for this client */;

        await cacheManager.InvalidateAsync(default, dependencies);
    }
}
```

## Best Practices

### 1. Choose Appropriate Expiration Times

```csharp
// Frequently changing content (news, live data)
.WithMemoryCache(defaultExpiration: TimeSpan.FromMinutes(5))

// Moderately dynamic content (blog posts, products)
.WithMemoryCache(defaultExpiration: TimeSpan.FromHours(1))

// Rarely changing content (about pages, navigation)
.WithMemoryCache(defaultExpiration: TimeSpan.FromHours(6))

// Very stable content (archived content, documentation)
.WithMemoryCache(defaultExpiration: TimeSpan.FromDays(1))
```

### 2. Implement Webhook Invalidation

Always use webhooks in production to keep cache fresh:
- Set up webhook endpoint
- Verify signatures
- Invalidate specific dependencies
- Log invalidation events

### 3. Monitor Cache Performance

```csharp
public class MonitoredCacheManager : IDeliveryCacheManager
{
    private readonly IDeliveryCacheManager _inner;
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;

    public async ValueTask<T?> TryGetAsync<T>(string key, ...)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await _inner.TryGetAsync<T>(key, cancellationToken);
        stopwatch.Stop();

        var hit = result != null;
        _metrics.RecordCacheAccess(hit, stopwatch.ElapsedMilliseconds);

        if (hit)
            _logger.LogDebug("Cache hit: {Key} in {Ms}ms", key, stopwatch.ElapsedMilliseconds);
        else
            _logger.LogDebug("Cache miss: {Key}", key);

        return result;
    }

    // ... implement other methods
}
```

### 4. Handle Cache Failures Gracefully

```csharp
public async ValueTask<T?> TryGetAsync<T>(string key, ...)
{
    try
    {
        return await _cache.GetAsync<T>(key, cancellationToken);
    }
    catch (RedisConnectionException ex)
    {
        _logger.LogWarning(ex, "Redis connection failed, bypassing cache");
        return default;  // Gracefully degrade to direct API calls
    }
}
```

### 5. Pre-Warm Cache

For critical content, pre-warm the cache on startup:

```csharp
public class CacheWarmupService : IHostedService
{
    private readonly IDeliveryClient _client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Pre-load homepage
        await _client.GetItem("homepage").ExecuteAsync(cancellationToken);

        // Pre-load navigation
        await _client.GetItem("main_navigation").ExecuteAsync(cancellationToken);

        // Pre-load recent articles
        await _client.GetItems<Article>()
            .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
            .OrderBy(ItemSystemPath.LastModified, descending: true)
            .Limit(10)
            .ExecuteAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

services.AddHostedService<CacheWarmupService>();
```

### 6. Use Stale-While-Revalidate Pattern

```csharp
public class StaleWhileRevalidateCache : IDeliveryCacheManager
{
    private readonly TimeSpan _staleThreshold = TimeSpan.FromMinutes(5);

    public async ValueTask<T?> TryGetAsync<T>(string key, ...)
    {
        var cached = await GetCachedValueWithTimestamp<T>(key);

        if (cached == null)
            return default;

        // If stale, trigger background refresh
        if (DateTime.UtcNow - cached.Timestamp > _staleThreshold)
        {
            _ = Task.Run(() => RefreshInBackground<T>(key));
        }

        return cached.Value;
    }
}
```

## Monitoring and Diagnostics

### Cache Hit Rate

```csharp
public class CacheMetrics
{
    private long _hits;
    private long _misses;

    public double HitRate => _hits + _misses == 0
        ? 0
        : (double)_hits / (_hits + _misses);

    public void RecordHit() => Interlocked.Increment(ref _hits);
    public void RecordMiss() => Interlocked.Increment(ref _misses);
}
```

### Cache Size Monitoring

```csharp
services.AddMemoryCache(options =>
{
    options.TrackStatistics = true;  // Enable statistics tracking
});

// Access statistics
var cache = serviceProvider.GetRequiredService<IMemoryCache>();
var stats = cache.GetCurrentStatistics();

Console.WriteLine($"Total hits: {stats.TotalHits}");
Console.WriteLine($"Total misses: {stats.TotalMisses}");
Console.WriteLine($"Current entry count: {stats.CurrentEntryCount}");
```

### Logging

```csharp
public class LoggingCacheManager : IDeliveryCacheManager
{
    private readonly IDeliveryCacheManager _inner;
    private readonly ILogger _logger;

    public async ValueTask<T?> TryGetAsync<T>(string key, ...)
    {
        var result = await _inner.TryGetAsync<T>(key, cancellationToken);

        _logger.LogInformation(
            "Cache {Result} for key: {Key}",
            result != null ? "HIT" : "MISS",
            key);

        return result;
    }

    public async ValueTask SetAsync<T>(string key, T value, ...)
    {
        await _inner.SetAsync(key, value, expiration, cancellationToken);

        _logger.LogInformation(
            "Cache SET: {Key}, Expiration: {Expiration}",
            key,
            expiration);
    }
}
```

## Troubleshooting

### Cache Not Working

**Problem**: Content is always fetched from API, not cache.

**Solutions**:

1. **Verify cache is registered**:
```csharp
var cacheManager = serviceProvider.GetService<IDeliveryCacheManager>();
if (cacheManager == null)
{
    // Cache not registered!
}
```

2. **Check cache expiration** isn't too short
3. **Verify queries are identical** (different parameters = different cache keys)

### Stale Content

**Problem**: Cache returns old content after updates.

**Solutions**:

1. **Implement webhook invalidation**
2. **Reduce cache expiration time**
3. **Manually invalidate** after content updates

### Memory Pressure

**Problem**: Application uses too much memory.

**Solutions**:

1. **Use distributed cache** instead of memory cache
2. **Configure cache size limits**:
```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;  // Limit number of entries
});
```
3. **Reduce expiration times**
4. **Be selective** about what you cache

### Redis Connection Failures

**Problem**: Application crashes when Redis is unavailable.

**Solutions**:

1. **Graceful degradation**:
```csharp
try
{
    return await _cache.GetAsync(key);
}
catch (RedisConnectionException)
{
    return default;  // Fall back to API
}
```

2. **Configure connection resilience**:
```csharp
var config = ConfigurationOptions.Parse("localhost:6379");
config.AbortOnConnectFail = false;
config.ConnectRetry = 3;
config.ReconnectRetryPolicy = new ExponentialRetry(5000);
```

---

**Related Documentation**:
- [Main README](../README.md)
- [Performance Optimization Guide](performance-optimization.md)
- [Multi-Client Scenarios](multi-client-scenarios.md)
