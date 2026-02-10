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
- [Per-Client Caching](#per-client-caching)
  - [Enabling Caching for Named Clients](#enabling-caching-for-named-clients)
  - [Cache Key Prefixing](#cache-key-prefixing)
  - [Distributed Cache for Named Clients](#distributed-cache-for-named-clients)
- [Multi-Tenant Caching](#multi-tenant-caching)
  - [Complete Multi-Tenant Example](#complete-multi-tenant-example)
  - [Per-Tenant Cache Invalidation](#per-tenant-cache-invalidation)
  - [Selective Caching (Production vs Preview)](#selective-caching-production-vs-preview)
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

**Note:** The SDK stores raw JSON payloads in distributed caches and rehydrates on read. This avoids circular reference serialization issues and keeps payloads portable across instances.

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

// Single client scenario - no name required
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
services.AddDeliveryMemoryCache(defaultExpiration: TimeSpan.FromHours(1));

var serviceProvider = services.BuildServiceProvider();
```

#### Named Clients

For multi-client scenarios, use named clients:

```csharp
services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "your-environment-id";
});

services.AddDeliveryMemoryCache("production", defaultExpiration: TimeSpan.FromMinutes(30));
```

#### Advanced Memory Cache Configuration

```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;  // Limit cache size
    options.CompactionPercentage = 0.25;  // Remove 25% when limit hit
});

services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "your-environment-id";
});

services.AddDeliveryMemoryCache("production", defaultExpiration: TimeSpan.FromHours(1));
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

// Single client scenario
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
services.AddDeliveryDistributedCache(defaultExpiration: TimeSpan.FromHours(2));

// Or with named clients for multi-client scenarios:
// services.AddDeliveryClient("production", options => { ... });
// services.AddDeliveryDistributedCache("production", defaultExpiration: TimeSpan.FromHours(2));
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

services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "your-environment-id";
});

services.AddDeliveryDistributedCache("production", defaultExpiration: TimeSpan.FromHours(4));
```

#### SQL Server Distributed Cache

```csharp
services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = configuration.GetConnectionString("CacheDb");
    options.SchemaName = "dbo";
    options.TableName = "KontentCache";
});

services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "your-environment-id";
});

services.AddDeliveryDistributedCache("production", defaultExpiration: TimeSpan.FromHours(1));
```

#### Azure Cache for Redis

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("AzureRedis");
    options.InstanceName = "Production_Kontent_";
});

services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "your-environment-id";
});

services.AddDeliveryDistributedCache("production", defaultExpiration: TimeSpan.FromHours(6));
```

### Custom Cache Manager

For advanced scenarios, implement a custom cache manager. Use `IDeliveryCacheManager` with the default `StorageMode` (`CacheStorageMode.HydratedObject`) for hydrated-object caching (memory), or override `StorageMode` to `CacheStorageMode.RawJson` for raw JSON payload caching (distributed).

#### Hydrated-object cache manager (memory style)

```csharp
using Kontent.Ai.Delivery.Abstractions;

public class CustomMemoryCacheManager : IDeliveryCacheManager
{
    // Implementation omitted - store hydrated objects directly (no serialization).
    public Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class
        => throw new NotImplementedException();

    public Task SetAsync<T>(
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
        => throw new NotImplementedException();

    public Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
        => throw new NotImplementedException();
}
```

#### Raw JSON cache manager (distributed style)

```csharp
using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

public class CustomDistributedCacheManager : IDeliveryCacheManager
{
    private readonly IDistributedCache _cache;

    public CustomDistributedCacheManager(IDistributedCache cache) => _cache = cache;

    // Tell the SDK to cache raw JSON payloads instead of hydrated objects
    public CacheStorageMode StorageMode => CacheStorageMode.RawJson;

    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class
    {
        var json = await _cache.GetStringAsync(cacheKey, cancellationToken);
        return json is null ? null : JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
        };

        var json = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);

        // Implement dependency index + invalidation for production use
    }

    public Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
        => Task.CompletedTask;
}

// Registration
services.AddSingleton<IDeliveryCacheManager, CustomDistributedCacheManager>();
```

## How Caching Works

### Cache Keys

Cache keys are automatically generated from query parameters using a deterministic, human-readable format.

#### Key Format

The general format is: `{queryType}:{identifier}:{params}:{filters}`

| Query Type | Format | Example |
|------------|--------|---------|
| Single Item | `item:{codename}:lang={lang}:depth={n}:elements={sorted}` | `item:homepage:lang=en-US:depth=2` |
| List Items | `items:lang={lang}:depth={n}:skip={n}:limit={n}:filters={hash}` | `items:lang=en-US:skip=0:limit=10` |
| Single Type | `type:{codename}:elements={sorted}` | `type:article:elements=name\|codename` |
| List Types | `types:skip={n}:limit={n}:elements={sorted}:filters={hash}` | `types:skip=0:limit=25` |
| Single Taxonomy | `taxonomy:{codename}` | `taxonomy:categories` |
| List Taxonomies | `taxonomies:skip={n}:limit={n}:filters={hash}` | `taxonomies:skip=0:limit=100` |

#### Key Properties

- **Deterministic**: Same parameters always produce the same key
- **Order-independent**: Arrays and filter dictionaries in different orders produce the same key
- **Human-readable**: Common parameters are visible for debugging (e.g., `lang=en-US:depth=2`)
- **Efficient**: Filters are hashed to keep keys compact when queries are complex

#### Examples

```csharp
// Single item query
await client.GetItem<Article>("homepage").ExecuteAsync();
// Key: item:homepage

// Item with language and depth
await client.GetItem<Article>("homepage")
    .WithLanguage("de-DE")
    .Depth(2)
    .ExecuteAsync();
// Key: item:homepage:lang=de-DE:depth=2

// Item with element projection
await client.GetItem<Article>("homepage")
    .WithElements("title", "description")
    .ExecuteAsync();
// Key: item:homepage:elements=description|title  (sorted alphabetically)

// Items listing with pagination
await client.GetItems<Article>()
    .Skip(10)
    .Limit(5)
    .ExecuteAsync();
// Key: items:skip=10:limit=5

// Items with filters (filters are hashed for brevity)
await client.GetItems<Article>()
    .Where(f => f.System("type").IsEqualTo("article"))
    .Where(f => f.Element("category").IsIn("news", "blog"))
    .ExecuteAsync();
// Key: items:filters=A7F3E2B9C1D5  (12-char hash of sorted filter parameters)

// Taxonomy query
await client.GetTaxonomy("categories").ExecuteAsync();
// Key: taxonomy:categories
```

#### Filter Hashing

When queries include filters, they are hashed using SHA256 (first 12 characters of URL-safe base64):

- Filters are sorted by key, then by value before hashing
- This ensures `{("a", "1"), ("b", "2")}` and `{("b", "2"), ("a", "1")}` produce the same hash
- The 12-character hash provides ~72 bits of entropy (extremely low collision probability)

#### Key Prefixing

**Default (single-client) scenario:**
```csharp
services.AddDeliveryClient(o => o.EnvironmentId = "...");
services.AddDeliveryMemoryCache();
// Keys have NO prefix: item:homepage, items:skip=0:limit=10, etc.
```

**Named clients (multi-client scenario):**
```csharp
services.AddDeliveryClient("production", o => o.EnvironmentId = "...");
services.AddDeliveryMemoryCache("production");
// Keys are prefixed with client name: production:item:homepage, etc.
```

**Custom prefix:**
```csharp
services.AddDeliveryMemoryCache("production", keyPrefix: "prod");
// Keys become: prod:item:homepage, prod:items:skip=0:limit=10, etc.
```

**No prefix (explicit):**
```csharp
services.AddDeliveryMemoryCache("production", keyPrefix: "");
// Keys have no prefix even for named clients
```

This prevents cache collisions when multiple clients share the same underlying cache.

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
services.AddDeliveryClient("production", options => { ... });
services.AddDeliveryMemoryCache("production", defaultExpiration: TimeSpan.FromHours(2));
```

#### Sliding Expiration

For custom cache managers, you can implement sliding expiration:

```csharp
public async Task SetAsync<T>(
    string key,
    T value,
    IEnumerable<string> dependencies,
    TimeSpan? expiration = null,
    CancellationToken cancellationToken = default) where T : class
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

### Purge All (Memory Cache)

Sometimes you need to invalidate **everything at once** (e.g., after a deployment, emergency rollback, or a major content model change).

The SDK exposes an **optional** capability interface `IDeliveryCachePurger` that is implemented by the in-memory cache manager (`MemoryCacheManager`).

> [!NOTE]
> Purge-all is not supported for generic distributed caches (`IDistributedCache`) because they don't provide key enumeration. For distributed caches, use provider-specific purge tooling or key-prefix rotation.

```csharp
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;

var cacheManager = serviceProvider.GetRequiredService<IDeliveryCacheManager>();

if (cacheManager is IDeliveryCachePurger purger)
{
    await purger.PurgeAsync();
}
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

## Per-Client Caching

The SDK supports per-client cache configuration using keyed services, allowing different named clients to have independent caching strategies.

### Enabling Caching for Named Clients

Use `AddDeliveryMemoryCache` or `AddDeliveryDistributedCache` to enable caching for specific named clients:

```csharp
// Register named clients
services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "production-environment-id";
});

services.AddDeliveryClient("preview", options =>
{
    options.EnvironmentId = "preview-environment-id";
    options.UsePreviewApi = true;
    options.PreviewApiKey = "your-preview-api-key";
});

// Enable caching ONLY for production client
services.AddDeliveryMemoryCache("production",
    keyPrefix: "prod",
    defaultExpiration: TimeSpan.FromHours(1));

// Preview client has no cache - always fetches fresh content
```

### Cache Key Prefixing

When multiple clients share the same underlying cache (e.g., same `IMemoryCache` or Redis instance), key prefixes prevent collisions:

```csharp
// Both clients share IMemoryCache but have isolated entries
services.AddDeliveryMemoryCache("client1", keyPrefix: "brand-a");
services.AddDeliveryMemoryCache("client2", keyPrefix: "brand-b");
```

Key prefixes are automatically applied to all cache keys and dependency tracking.

### Distributed Cache for Named Clients

```csharp
// Register distributed cache implementation
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Register client
services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "production-environment-id";
});

// Enable distributed caching for the client
services.AddDeliveryDistributedCache("production",
    keyPrefix: "prod",
    defaultExpiration: TimeSpan.FromHours(2));
```

## Multi-Tenant Caching

When serving multiple environments or brands, use per-client caching with distinct key prefixes:

### Complete Multi-Tenant Example

```csharp
// Register tenant clients
services.AddDeliveryClient("tenant-a", options =>
{
    options.EnvironmentId = "tenant-a-environment-id";
});

services.AddDeliveryClient("tenant-b", options =>
{
    options.EnvironmentId = "tenant-b-environment-id";
});

// Configure caching for each tenant (order doesn't matter)
services.AddDeliveryMemoryCache("tenant-a", keyPrefix: "tenant-a");
services.AddDeliveryMemoryCache("tenant-b", keyPrefix: "tenant-b");

// Access clients via factory
var factory = serviceProvider.GetRequiredService<IDeliveryClientFactory>();
var tenantAClient = factory.Get("tenant-a");
var tenantBClient = factory.Get("tenant-b");
```

### Per-Tenant Cache Invalidation

```csharp
public class TenantCacheService
{
    private readonly IServiceProvider _serviceProvider;

    public async Task InvalidateTenantCacheAsync(string tenantId, params string[] dependencies)
    {
        // Get the keyed cache manager for the specific tenant
        var cacheManager = _serviceProvider.GetKeyedService<IDeliveryCacheManager>(tenantId);

        if (cacheManager != null)
        {
            await cacheManager.InvalidateAsync(default, dependencies);
        }
    }
}
```

### Selective Caching (Production vs Preview)

A common pattern is to cache production content but not preview content:

```csharp
// Production: cached for performance
services.AddDeliveryMemoryCache("production",
    keyPrefix: "prod",
    defaultExpiration: TimeSpan.FromHours(2));
services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "your-environment-id";
});

// Preview: no caching for fresh content during editing
services.AddDeliveryClient("preview", options =>
{
    options.EnvironmentId = "your-environment-id";
    options.UsePreviewApi = true;
    options.PreviewApiKey = "your-preview-api-key";
});
// No AddDeliveryMemoryCache for "preview" = no caching
```

## Best Practices

### 1. Choose Appropriate Expiration Times

```csharp
// Frequently changing content (news, live data)
services.AddDeliveryMemoryCache("news-client", defaultExpiration: TimeSpan.FromMinutes(5));

// Moderately dynamic content (blog posts, products)
services.AddDeliveryMemoryCache("blog-client", defaultExpiration: TimeSpan.FromHours(1));

// Rarely changing content (about pages, navigation)
services.AddDeliveryMemoryCache("static-client", defaultExpiration: TimeSpan.FromHours(6));

// Very stable content (archived content, documentation)
services.AddDeliveryMemoryCache("docs-client", defaultExpiration: TimeSpan.FromDays(1));
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

    public async Task<T?> GetAsync<T>(string key, ...)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await _inner.GetAsync<T>(key, cancellationToken);
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
    public async Task<T?> GetAsync<T>(string key, ...)
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
            .Where(f => f.System("type").IsEqualTo("article"))
            .OrderBy("system.last_modified", OrderingMode.Descending)
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

    public async Task<T?> GetAsync<T>(string key, ...)
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

### 7. Prevent Cache Stampede (Request Coalescing)

In high-traffic scenarios, a popular cache key can expire (or be invalidated) and cause many concurrent requests to miss the cache at the same time. If every request then calls the Delivery API, you get a spike of redundant calls (the "thundering herd" problem).

The SDK mitigates this for cached query execution by **coalescing concurrent cache misses**:
- The first request performs the API call and populates the cache
- Concurrent requests for the same cache key wait for the first request to finish (then read the cached result)

Implementation details:
- Coalescing is **scoped per `IDeliveryCacheManager` instance** (so different named clients / cache managers do not block each other)
- Lock entries are cleaned up automatically to avoid unbounded growth in long-running applications

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

    public async Task<T?> GetAsync<T>(string key, ...)
    {
        var result = await _inner.GetAsync<T>(key, cancellationToken);

        _logger.LogInformation(
            "Cache {Result} for key: {Key}",
            result != null ? "HIT" : "MISS",
            key);

        return result;
    }

    public async Task SetAsync<T>(string key, T value, IEnumerable<string> dependencies, TimeSpan? expiration = null, ...)
    {
        await _inner.SetAsync(key, value, dependencies, expiration, cancellationToken);

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
