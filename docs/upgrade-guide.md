# Upgrade Guide: Migrating to Kontent.ai Delivery SDK 19.0

This guide provides comprehensive instructions for migrating from the legacy Kontent.ai Delivery SDK (18.x) to the new 19.0 SDK. The new SDK features a modernized API with fluent query builders, integrated caching, Polly-based resilience, and improved type safety.

## Table of Contents

- [Overview](#overview)
- [Quick Migration Checklist](#quick-migration-checklist)
- [1. Migrating Query Methods](#1-migrating-query-methods)
- [2. Migrating Filtering](#2-migrating-filtering)
- [3. Migrating Caching](#3-migrating-caching)
- [4. Migrating Retry Policies](#4-migrating-retry-policies)
- [5. Migrating Rich Text Resolution](#5-migrating-rich-text-resolution)
- [6. Migrating Response Handling](#6-migrating-response-handling)
- [7. Migrating Model Structure](#7-migrating-model-structure)
- [8. Migrating DI Registration](#8-migrating-di-registration)
- [9. Removed Features & Interfaces](#9-removed-features--interfaces)
- [10. New Features](#10-new-features)
- [11. Migrating the Sync API](#11-migrating-the-sync-api)
- [Troubleshooting](#troubleshooting)

## Overview

### Version Requirements

- **.NET 8.0+** is required (upgraded from .NET 6.0 support)
- **Model Generator 10.0.0** is required for strongly-typed models

### Package Changes

| Legacy | New |
|--------|-----|
| `Kontent.Ai.Delivery` | `Kontent.Ai.Delivery` (same package, new version) |
| `Kontent.Ai.Delivery.Caching` | `Kontent.Ai.Delivery.Caching` (new FusionCache-backed implementation, different API) |
| `Kontent.Ai.Delivery.Abstractions` | `Kontent.Ai.Delivery.Abstractions` (updated interfaces) |

### Summary of Breaking Changes

| Area | Change Type | Description |
|------|-------------|-------------|
| Query Methods | Breaking | `GetItemAsync<T>()` → `GetItem<T>().ExecuteAsync()` builder pattern |
| Filtering | Breaking | Parameter classes → Fluent `Where()` syntax |
| Caching | Breaking | Separate package → New FusionCache-backed implementation in `Kontent.Ai.Delivery.Caching` |
| Retry Policies | Breaking | `IRetryPolicy` → Polly-based `configureResilience` |
| Rich Text | Breaking | `IContentLinkUrlResolver` → `HtmlResolverBuilder` |
| Response Types | Breaking | Direct response → Result pattern with `IsSuccess`/`Value`/`Error` |
| Model Structure | Breaking | Flat properties → `IContentItem<T>` wrapper |
| DI Registration | Moderate | New overloads, keyed services support |
| Content Freshness | Breaking | Global `DeliveryOptions.WaitForLoadingNewContent` removed; use per-query `WaitForLoadingNewContent(true)` |

## Quick Migration Checklist

Use this checklist to ensure you've addressed all required changes:

- [ ] Update NuGet packages to 19.0.0+
- [ ] Update `Kontent.Ai.Delivery.Caching` package reference to 19.0.0+ (new FusionCache-backed implementation)
- [ ] Regenerate models with model generator 10.0.0
- [ ] Refactor all `GetItemAsync<T>()` calls to `GetItem<T>().ExecuteAsync()`
- [ ] Refactor all `GetItemsAsync<T>()` calls to `GetItems<T>().ExecuteAsync()`
- [ ] Migrate all filtering from parameter classes to fluent `Where()` syntax
- [ ] Update caching registration from `AddDeliveryClientCache()` to `AddDeliveryMemoryCache()` or `AddDeliveryHybridCache()`
- [ ] Replace `IContentLinkUrlResolver` implementations with `HtmlResolverBuilder`
- [ ] Replace `IInlineContentItemsResolver<T>` implementations with `HtmlResolverBuilder`
- [ ] Update response handling to use result pattern (`IsSuccess`, `Value`, `Error`)
- [ ] Update content access from `response.Item.Title` to `result.Value.Elements.Title`
- [ ] Migrate custom retry policies from `IRetryPolicy` to Polly configuration
- [ ] If you used the Sync API (`PostSyncInitAsync`/`GetSyncAsync`/V2), migrate to the standalone [`Kontent.Ai.Sync`](https://github.com/kontent-ai/sync-sdk-net) package

---

## 1. Migrating Query Methods

The SDK now uses a fluent builder pattern for all queries. Instead of calling async methods directly with parameters, you build a query and then execute it.

### 1.1 Single Item Queries

**Legacy:**
```csharp
// Basic single item retrieval
var response = await client.GetItemAsync<Article>("article_codename");
var title = response.Item.Title;

// With parameters
var response = await client.GetItemAsync<Article>("article_codename",
    new LanguageParameter("es-ES"),
    new DepthParameter(2),
    new ElementsParameter("title", "summary", "body"));
```

**New:**
```csharp
// Basic single item retrieval
var result = await client.GetItem<Article>("article_codename").ExecuteAsync();

if (result.IsSuccess)
{
    var title = result.Value.Elements.Title;
}

// With parameters (fluent builder)
var result = await client.GetItem<Article>("article_codename")
    .WithLanguage("es-ES")
    .Depth(2)
    .WithElements("title", "summary", "body")
    .ExecuteAsync();
```

### 1.2 Multiple Items Queries

**Legacy:**
```csharp
// Basic items retrieval
var response = await client.GetItemsAsync<Article>();
foreach (var item in response.Items)
{
    Console.WriteLine(item.Title);
}

// With pagination and filtering
var response = await client.GetItemsAsync<Article>(
    new LanguageParameter("es-ES"),
    new LimitParameter(10),
    new SkipParameter(20),
    new OrderParameter("elements.post_date", SortOrder.Descending),
    new EqualsFilter("system.type", "article"));
```

**New:**
```csharp
// Basic items retrieval
var result = await client.GetItems<Article>().ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var item in result.Value.Items)
    {
        Console.WriteLine(item.Elements.Title);
    }
}

// With pagination and filtering (fluent builder)
var result = await client.GetItems<Article>()
    .WithLanguage("es-ES")
    .Limit(10)
    .Skip(20)
    .OrderBy("elements.post_date", OrderingMode.Descending)
    .Where(f => f.System("type").IsEqualTo("article"))
    .ExecuteAsync();
```

### 1.3 Dynamic (Untyped) Queries

**Legacy:**
```csharp
// Dynamic item retrieval
var response = await client.GetItemAsync("homepage");
var systemName = response.Item.System.Name;
```

**New:**
```csharp
// Dynamic item retrieval
var result = await client.GetItem("homepage").ExecuteAsync();

if (result.IsSuccess)
{
    var systemName = result.Value.System.Name;
    // Access elements via dictionary-like interface
    if (result.Value.Elements.TryGetValue("title", out var title))
    {
        Console.WriteLine(title);
    }
}
```

### 1.4 Items Feed (Pagination)

**Legacy:**
```csharp
// Manual pagination
string continuationToken = null;
do
{
    var response = await client.GetItemsAsync<Article>(
        new LimitParameter(100),
        continuationToken != null ? new ContinuationTokenParameter(continuationToken) : null);

    foreach (var item in response.Items)
    {
        ProcessItem(item);
    }

    continuationToken = response.ContinuationToken;
} while (!string.IsNullOrEmpty(continuationToken));
```

**New:**
```csharp
// Option 1: Automatic async enumeration (recommended for streaming)
await foreach (var item in client.GetItemsFeed<Article>().EnumerateAsync())
{
    ProcessItem(item);
}

// Option 2: Manual page-by-page control
var firstPage = await client.GetItemsFeed<Article>().ExecuteAsync();

if (firstPage.IsSuccess)
{
    foreach (var item in firstPage.Value.Items)
    {
        ProcessItem(item);
    }

    // Fetch next page if available
    while (firstPage.Value.HasNextPage)
    {
        var nextPage = await firstPage.Value.FetchNextPageAsync();
        if (nextPage?.IsSuccess == true)
        {
            foreach (var item in nextPage.Value.Items)
            {
                ProcessItem(item);
            }
            firstPage = nextPage;
        }
        else break;
    }
}
```

### 1.5 Content Types

**Legacy:**
```csharp
// Single type
var response = await client.GetTypeAsync("article");
var typeName = response.Type.System.Name;

// All types
var response = await client.GetTypesAsync(
    new LimitParameter(10),
    new SkipParameter(0));
```

**New:**
```csharp
// Single type
var result = await client.GetType("article").ExecuteAsync();

if (result.IsSuccess)
{
    var typeName = result.Value.System.Name;
}

// All types
var result = await client.GetTypes()
    .Limit(10)
    .Skip(0)
    .ExecuteAsync();
```

### 1.6 Taxonomies

**Legacy:**
```csharp
// Single taxonomy
var response = await client.GetTaxonomyAsync("categories");

// All taxonomies
var response = await client.GetTaxonomiesAsync(
    new LimitParameter(10));
```

**New:**
```csharp
// Single taxonomy
var result = await client.GetTaxonomy("categories").ExecuteAsync();

// All taxonomies
var result = await client.GetTaxonomies()
    .Limit(10)
    .ExecuteAsync();
```

### 1.7 Languages

**Legacy:**
```csharp
var response = await client.GetLanguagesAsync();
```

**New:**
```csharp
var result = await client.GetLanguages().ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var language in result.Value.Languages)
    {
        Console.WriteLine($"{language.System.Name} ({language.System.Codename})");
    }
}
```

### 1.8 Content Element

**Legacy:**
```csharp
var response = await client.GetContentElementAsync("article", "title");
```

**New:**
```csharp
var result = await client.GetContentElement("article", "title").ExecuteAsync();

if (result.IsSuccess)
{
    Console.WriteLine($"Element: {result.Value.Name}");
    Console.WriteLine($"Type: {result.Value.Type}");
}
```

---

## 2. Migrating Filtering

The SDK replaces individual filter parameter classes with a fluent filter builder accessed via the `Where()` method.

### 2.1 Filter Parameter to Fluent API Mapping

| Legacy Parameter | New Fluent API |
|-----------------|----------------|
| `EqualsFilter("system.type", "article")` | `.System("type").IsEqualTo("article")` |
| `NotEqualsFilter("system.type", "article")` | `.System("type").IsNotEqualTo("article")` |
| `LessThanFilter("elements.price", "100")` | `.Element("price").IsLessThan(100.0)` |
| `LessThanOrEqualFilter("elements.price", "100")` | `.Element("price").IsLessThanOrEqualTo(100.0)` |
| `GreaterThanFilter("elements.price", "50")` | `.Element("price").IsGreaterThan(50.0)` |
| `GreaterThanOrEqualFilter("elements.price", "50")` | `.Element("price").IsGreaterThanOrEqualTo(50.0)` |
| `RangeFilter("elements.price", "50", "100")` | `.Element("price").IsWithinRange(50.0, 100.0)` |
| `InFilter("system.type", "article", "blog")` | `.System("type").IsIn("article", "blog")` |
| `NotInFilter("system.type", "article", "blog")` | `.System("type").IsNotIn("article", "blog")` |
| `ContainsFilter("elements.tags", "featured")` | `.Element("tags").Contains("featured")` |
| `AnyFilter("elements.tags", "featured", "popular")` | `.Element("tags").ContainsAny("featured", "popular")` |
| `AllFilter("elements.tags", "featured", "popular")` | `.Element("tags").ContainsAll("featured", "popular")` |
| `EmptyFilter("elements.description")` | `.Element("description").IsEmpty()` |
| `NotEmptyFilter("elements.description")` | `.Element("description").IsNotEmpty()` |

### 2.2 System Property Filtering

**Legacy:**
```csharp
var response = await client.GetItemsAsync<Article>(
    new EqualsFilter("system.type", "article"),
    new EqualsFilter("system.language", "en-US"),
    new NotEqualsFilter("system.collection", "archived"),
    new GreaterThanFilter("system.last_modified", "2024-01-01T00:00:00Z"));
```

**New:**
```csharp
var result = await client.GetItems<Article>()
    .Where(f => f
        .System("type").IsEqualTo("article")
        .System("language").IsEqualTo("en-US")
        .System("collection").IsNotEqualTo("archived")
        .System("last_modified").IsGreaterThan(new DateTime(2024, 1, 1)))
    .ExecuteAsync();
```

### 2.3 Element Property Filtering

**Legacy:**
```csharp
var response = await client.GetItemsAsync<Product>(
    new EqualsFilter("elements.category", "electronics"),
    new GreaterThanFilter("elements.price", "100"),
    new ContainsFilter("elements.tags", "featured"));
```

**New:**
```csharp
var result = await client.GetItems<Product>()
    .Where(f => f
        .Element("category").Contains("electronics")
        .Element("price").IsGreaterThan(100.0)
        .Element("tags").Contains("featured"))
    .ExecuteAsync();
```

### 2.4 Combining Multiple Filters

Filters in the new SDK use AND semantics when chained. For conditional filtering, build the query incrementally:

**Legacy:**
```csharp
var parameters = new List<IQueryParameter>();
parameters.Add(new EqualsFilter("system.type", "article"));

if (filterByCategory)
{
    parameters.Add(new ContainsFilter("elements.category", selectedCategory));
}

if (filterByDate)
{
    parameters.Add(new GreaterThanFilter("elements.post_date", startDate.ToString("o")));
}

var response = await client.GetItemsAsync<Article>(parameters.ToArray());
```

**New:**
```csharp
var query = client.GetItems<Article>()
    .Where(f => f.System("type").IsEqualTo("article"));

if (filterByCategory)
{
    query = query.Where(f => f.Element("category").Contains(selectedCategory));
}

if (filterByDate)
{
    query = query.Where(f => f.Element("post_date").IsGreaterThan(startDate));
}

var result = await query.ExecuteAsync();
```

### 2.5 Type-Safe Filter Values

The new filter builder accepts typed values directly:

```csharp
// Strings
.Element("title").IsEqualTo("Welcome")

// Numbers (double)
.Element("price").IsGreaterThan(99.99)
.Element("quantity").IsWithinRange(1.0, 100.0)

// DateTime
.System("last_modified").IsGreaterThan(DateTime.UtcNow.AddDays(-30))
.Element("publish_date").IsLessThan(DateTime.Now)

// Multiple choice (by codename)
.Element("status").Contains("published")

// Arrays (for In/NotIn operators)
.System("type").IsIn("article", "blog_post", "news")
.Element("category").ContainsAny("tech", "science", "health")
```

---

## 3. Migrating Caching

Caching remains a standalone package (`Kontent.Ai.Delivery.Caching`) but the implementation has been rewritten on top of [FusionCache](https://github.com/ZiggyCreatures/FusionCache) with a simplified API.

### 3.1 Package Changes

**Legacy:**
```xml
<PackageReference Include="Kontent.Ai.Delivery" Version="18.x.x" />
<PackageReference Include="Kontent.Ai.Delivery.Caching" Version="18.x.x" />
```

**New:**
```xml
<PackageReference Include="Kontent.Ai.Delivery" Version="19.0.0" />
<PackageReference Include="Kontent.Ai.Delivery.Caching" Version="19.0.0" />
```

### 3.2 Memory Cache Registration

**Legacy:**
```csharp
services.AddDeliveryClient(Configuration);
services.AddDeliveryClientCache(new DeliveryCacheOptions
{
    CacheType = CacheTypeEnum.Memory,
    DefaultExpiration = TimeSpan.FromHours(1)
});
```

**New:**
```csharp
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
services.AddDeliveryMemoryCache(defaultExpiration: TimeSpan.FromHours(1));
```

### 3.3 Hybrid Cache Registration (Redis)

**Legacy:**
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

services.AddDeliveryClient(Configuration);
services.AddDeliveryClientCache(new DeliveryCacheOptions
{
    CacheType = CacheTypeEnum.Distributed,
    DefaultExpiration = TimeSpan.FromHours(2)
});
```

**New:**
```csharp
// First, register distributed cache implementation
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Then register delivery client with hybrid cache
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
services.AddDeliveryHybridCache(defaultExpiration: TimeSpan.FromHours(2));
```

> [!NOTE]
> Built-in cache registrations (in the `Kontent.Ai.Delivery.Caching` package) are FusionCache-backed internally. Hybrid caching stores raw JSON payloads to avoid serialization issues with hydrated object graphs. If you implement a custom hybrid cache manager, override `StorageMode` to return `CacheStorageMode.RawJson` so the SDK uses the raw JSON caching path.

### 3.4 Named Client Caching

**Legacy:**
```csharp
services.AddDeliveryClient("production", Configuration, "ProductionDeliveryOptions");
services.AddDeliveryClientCache("production", new DeliveryCacheOptions
{
    CacheType = CacheTypeEnum.Memory,
    DefaultExpiration = TimeSpan.FromHours(1)
});
```

**New:**
```csharp
services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "production-environment-id";
});
services.AddDeliveryMemoryCache("production", defaultExpiration: TimeSpan.FromHours(1));
```

### 3.5 DeliveryCacheOptions Migration

The legacy `DeliveryCacheOptions` (with `CacheType`, `StaleContentExpiration`, `DistributedCacheResilientPolicy`) is gone. A new `DeliveryCacheOptions` shaped around FusionCache lives in `Kontent.Ai.Delivery.Abstractions` and is exposed via the `Action<DeliveryCacheOptions>` overloads of `AddDeliveryMemoryCache()` / `AddDeliveryHybridCache()`.

**Mapping legacy → new:**

| Legacy Option | New Equivalent |
|--------------|----------------|
| `CacheType = CacheTypeEnum.Memory` | `AddDeliveryMemoryCache()` |
| `CacheType = CacheTypeEnum.Distributed` | `AddDeliveryHybridCache()` |
| `DefaultExpiration` | `DeliveryCacheOptions.DefaultExpiration` (default `1h`) — pass via parameter or configure action. |
| `StaleContentExpiration` | Replaced by the per-result `HasStaleContent` flag and FusionCache's eager-refresh / fail-safe mechanisms. |
| `DistributedCacheResilientPolicy` | Removed — handled by Polly resilience pipeline + FusionCache fail-safe. |

**New options you can configure** (via `Action<DeliveryCacheOptions>`):

| Property | Default | Purpose |
|----------|---------|---------|
| `DefaultExpiration` | `1h` | Manager-wide TTL for cached entries (per-query override available via `.WithCacheExpiration(...)`). |
| `KeyPrefix` | `null` | Prefix added to every cache key. Use to isolate multiple clients sharing a cache store. |
| `IsFailSafeEnabled` | `false` | When `true`, FusionCache may serve stale entries if the API call fails (resilience during outages). |
| `FailSafeMaxDuration` | `1d` | Maximum age a stale entry may be served when fail-safe is active. |
| `FailSafeThrottleDuration` | `30s` | Minimum delay between background refresh attempts while fail-safe is active. |
| `JitterMaxDuration` | `0` | Random jitter added to expirations to spread load and prevent thundering-herd. |
| `EagerRefreshThreshold` | `0` | Fraction of TTL (0–1). When set, FusionCache refreshes entries in the background once this fraction has elapsed. |
| `ConfigureFusionCacheOptions` | `null` | Escape hatch — receives the underlying `FusionCacheOptions` for advanced FusionCache features (backplane, background ops, etc.). |

```csharp
services.AddDeliveryMemoryCache(opts =>
{
    opts.DefaultExpiration = TimeSpan.FromMinutes(10);
    opts.JitterMaxDuration = TimeSpan.FromSeconds(30);
    opts.IsFailSafeEnabled = true;
    opts.FailSafeMaxDuration = TimeSpan.FromHours(6);
    opts.EagerRefreshThreshold = 0.8f;
});
```

### 3.6 Cache Invalidation

**Legacy:**
```csharp
using Microsoft.Extensions.DependencyInjection;

var cacheManager = serviceProvider.GetRequiredService<IDeliveryCacheManager>();

// Invalidate by item codename
cacheManager.InvalidateEntry(CacheHelpers.GetItemKey("article_codename"));

// Invalidate by dependency key
cacheManager.InvalidateEntry(CacheHelpers.GetItemsDependencyKey());
```

**New:**
```csharp
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;

var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("production");

// Invalidate by dependency keys (items, assets, taxonomies)
await cacheManager.InvalidateAsync(["item_article_codename", "item_related_article"]);

// Webhook-based invalidation example
var dependencyKeys = webhookPayload.Data.Items
    .Select(i => $"item_{i.Codename}")
    .Append(DeliveryCacheDependencies.ItemsListScope)
    .ToArray();
await cacheManager.InvalidateAsync(dependencyKeys);

// Type and taxonomy events
await cacheManager.InvalidateAsync([$"type_{typeCodename}", DeliveryCacheDependencies.TypesListScope]);
await cacheManager.InvalidateAsync([$"taxonomy_{taxonomyCodename}", DeliveryCacheDependencies.TaxonomiesListScope]);
```

**Custom cache manager migration (keyed):**
```csharp
// Legacy (unkeyed, no longer used by SDK client resolution)
services.AddSingleton<IDeliveryCacheManager, CustomHybridCacheManager>();

// New (keyed per client)
services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "production-environment-id";
});
services.AddDeliveryCacheManager("production",
    sp => new CustomHybridCacheManager(sp.GetRequiredService<IDistributedCache>()));
```

**Dependency Key Format:**
| Entity Type | Key Format | Example |
|------------|------------|---------|
| Content Item | `item_{codename}` | `item_homepage` |
| Asset | `asset_{id}` | `asset_a5e1c4b2-1234-...` |
| Content Type | `type_{codename}` | `type_article` |
| Taxonomy | `taxonomy_{group}` | `taxonomy_categories` |
| Item list scope | `scope_items_list` (`DeliveryCacheDependencies.ItemsListScope`) | `scope_items_list` |
| Type list scope | `scope_types_list` (`DeliveryCacheDependencies.TypesListScope`) | `scope_types_list` |
| Taxonomy list scope | `scope_taxonomies_list` (`DeliveryCacheDependencies.TaxonomiesListScope`) | `scope_taxonomies_list` |

### 3.7 Cache Purge (Built-in Cache Managers)

**Legacy:**
```csharp
// No built-in purge-all functionality
```

**New:**
```csharp
var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("production");

if (cacheManager is IDeliveryCachePurger purger)
{
    await purger.PurgeAsync(); // permanently removes all entries

    // Or: expire entries but keep fail-safe fallback data
    await purger.PurgeAsync(allowFailSafe: true);
}
```

> [!NOTE]
> Custom cache managers may choose not to implement `IDeliveryCachePurger`. In that case, use provider-specific purge mechanisms.

### 3.8 Detecting Cache Hits

**New feature:** The SDK exposes both a coarse `IsCacheHit` flag and a finer-grained `ResponseSource` enum (`Origin`, `Cdn`, `Cache`, `FailSafe`):

```csharp
var result = await client.GetItem<Article>("homepage").ExecuteAsync();

if (result.IsSuccess)
{
    switch (result.ResponseSource)
    {
        case ResponseSource.Cache:
            Console.WriteLine("Served from SDK cache");
            break;
        case ResponseSource.FailSafe:
            Console.WriteLine("Served stale data (API call failed; fail-safe served the previous value)");
            break;
        case ResponseSource.Cdn:
            Console.WriteLine("Served from CDN edge cache (X-Cache: HIT)");
            break;
        case ResponseSource.Origin:
            Console.WriteLine($"Fetched from origin: {result.RequestUrl}");
            break;
    }
}
```

`IsCacheHit` returns `true` for both `Cache` and `FailSafe`. `ResponseHeaders` and `RequestUrl` are `null` whenever the response was served from the SDK cache (i.e., the API was not actually called).

### 3.9 Using DeliveryClientBuilder with Caching (Non-DI)

**Legacy:**
```csharp
var client = DeliveryClientBuilder.WithEnvironmentId("env-id").Build();
var cacheOptions = Options.Create(new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromHours(2) });
var memoryOptions = Options.Create(new MemoryCacheOptions());
var cachedClient = new DeliveryClientCache(
    CacheManagerFactory.Create(new MemoryCache(memoryOptions), cacheOptions),
    client);
```

**New:**
```csharp
await using var client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithEnvironmentId("your-environment-id")
        .Build())
    .WithMemoryCache(TimeSpan.FromHours(2))
    .Build();
```

### 3.10 Per-query Cache Expiration

`WithCacheExpiration(TimeSpan?)` is an extension method shipped in `Kontent.Ai.Delivery` that overrides the cache TTL for a single cacheable query without affecting the manager-level default:

```csharp
var result = await client.GetItem<Article>("homepage")
    .WithCacheExpiration(TimeSpan.FromMinutes(5))
    .ExecuteAsync();
```

It is available on:
- `GetItem<T>()`
- `GetItems<T>()`
- `GetType()`
- `GetTypes()`
- `GetTaxonomy()`
- `GetTaxonomies()`

Pass `null` to fall back to the manager's `DeliveryCacheOptions.DefaultExpiration`.

---

## 4. Migrating Retry Policies

The SDK now uses Polly for resilience instead of custom interfaces.

### 4.1 Interface Changes

**Legacy interfaces (removed):**
- `IRetryPolicy`
- `IRetryPolicyProvider`
- `DefaultRetryPolicyOptions`

**New:** Polly's `ResiliencePipelineBuilder<HttpResponseMessage>` via the `configureResilience` callback.

### 4.2 Default Retry Policy

The SDK includes a default retry policy that handles:
- Status codes: 408, 429, 500, 502, 503, 504
- Automatic Retry-After header support for 429 responses
- Exponential backoff with jitter
- 30-second timeout

### 4.3 Disabling Retry Policy

**Legacy:** Set `EnableRetryPolicy = false` on `DeliveryOptions`.
```csharp
services.AddDeliveryClient(new DeliveryOptions
{
    EnvironmentId = "env-id",
    EnableRetryPolicy = false
});
```

**New:** Use `DisableRetryPolicy()` on the options builder (it sets `EnableResilience = false` internally).
```csharp
services.AddDeliveryClient(builder => builder
    .WithEnvironmentId("env-id")
    .DisableRetryPolicy()
    .Build());
```

### 4.4 Configuration Migration

**Legacy:**
```csharp
services.AddDeliveryClient(builder => builder
    .WithEnvironmentId("env-id")
    .WithDefaultRetryPolicyOptions(new DefaultRetryPolicyOptions
    {
        DeltaBackoff = TimeSpan.FromSeconds(1),
        MaxCumulativeWaitTime = TimeSpan.FromSeconds(30)
    })
    .Build());
```

**New:**
```csharp
services.AddDeliveryClient(
    buildDeliveryOptions: builder => builder
        .WithEnvironmentId("env-id")
        .Build(),
    configureResilience: builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        });
    });
```

### 4.5 Custom Retry Policy Migration

**Legacy:**
```csharp
public class CustomRetryPolicy : IRetryPolicy
{
    public Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        // Custom retry logic
    }
}

public class CustomRetryPolicyProvider : IRetryPolicyProvider
{
    public IRetryPolicy GetRetryPolicy() => new CustomRetryPolicy();
}

// Registration
services.AddSingleton<IRetryPolicyProvider, CustomRetryPolicyProvider>();
services.AddDeliveryClient(Configuration);
```

**New:**
```csharp
services.AddDeliveryClient(
    buildDeliveryOptions: builder => builder
        .WithEnvironmentId("env-id")
        .Build(),
    configureResilience: builder =>
    {
        // Custom retry logic using Polly
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = args =>
            {
                // Custom retry condition
                var shouldRetry = args.Outcome.Result?.StatusCode == HttpStatusCode.ServiceUnavailable
                    || args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests;
                return ValueTask.FromResult(shouldRetry);
            },
            OnRetry = args =>
            {
                // Custom logging or metrics
                Console.WriteLine($"Retry attempt {args.AttemptNumber}");
                return ValueTask.CompletedTask;
            }
        });

        // Add circuit breaker
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30)
        });

        // Add timeout
        builder.AddTimeout(TimeSpan.FromSeconds(60));
    });
```

### 4.6 Default Retry Policy Options Mapping

| Legacy Option | Polly Equivalent |
|--------------|------------------|
| `DeltaBackoff` | `Delay` in `HttpRetryStrategyOptions` |
| `MaxCumulativeWaitTime` | Calculate `MaxRetryAttempts` based on backoff formula |
| Retry-After header support | Built-in via `DelayGenerator` |

---

## 5. Migrating Rich Text Resolution

Rich text resolution has been completely redesigned using a fluent builder pattern.

### 5.1 Content Link Resolution

**Legacy:**
```csharp
public class CustomContentLinkUrlResolver : IContentLinkUrlResolver
{
    public Task<string> ResolveLinkUrlAsync(IContentLink link)
    {
        if (link.ContentTypeCodename == "article")
        {
            return Task.FromResult($"/articles/{link.UrlSlug}");
        }
        if (link.ContentTypeCodename == "product")
        {
            return Task.FromResult($"/shop/{link.UrlSlug}");
        }
        return Task.FromResult($"/content/{link.Codename}");
    }

    public Task<string> ResolveBrokenLinkUrlAsync()
    {
        return Task.FromResult("/404");
    }
}

// Registration
services.AddSingleton<IContentLinkUrlResolver, CustomContentLinkUrlResolver>();
services.AddDeliveryClient(Configuration);
```

**New:**
```csharp
// Create resolver at point of use
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver("article", async (link, resolveChildren) =>
    {
        var innerHtml = await resolveChildren(link.Children);
        return $"<a href=\"/articles/{link.Metadata?.UrlSlug}\">{innerHtml}</a>";
    })
    .WithContentItemLinkResolver("product", async (link, resolveChildren) =>
    {
        var innerHtml = await resolveChildren(link.Children);
        return $"<a href=\"/shop/{link.Metadata?.UrlSlug}\">{innerHtml}</a>";
    })
    // Global fallback resolver
    .WithContentItemLinkResolver(async (link, resolveChildren) =>
    {
        var url = link.Metadata?.UrlSlug != null
            ? $"/content/{link.Metadata.UrlSlug}"
            : $"/content/{link.ItemId}";
        var innerHtml = await resolveChildren(link.Children);
        return $"<a href=\"{url}\">{innerHtml}</a>";
    })
    .Build();

// Use when rendering rich text
var html = await article.BodyCopy.ToHtmlAsync(resolver);
```

### 5.2 Inline Content Items (Components)

**Legacy:**
```csharp
public class TweetResolver : IInlineContentItemsResolver<Tweet>
{
    public Task<string> ResolveAsync(Tweet data)
    {
        return Task.FromResult(
            $"<blockquote class=\"twitter-tweet\">" +
            $"<p>{data.TweetText}</p>" +
            $"<cite>@{data.Handle}</cite>" +
            $"</blockquote>");
    }
}

// Registration
services.AddSingleton<IInlineContentItemsResolver<Tweet>, TweetResolver>();
services.AddDeliveryClient(Configuration);
```

**New (Type-Safe):**
```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver<Tweet>(tweet =>
        $"<blockquote class=\"twitter-tweet\">" +
        $"<p>{tweet.Elements.TweetText}</p>" +
        $"<cite>@{tweet.Elements.Handle}</cite>" +
        $"</blockquote>")
    .Build();

var html = await article.BodyCopy.ToHtmlAsync(resolver);
```

**New (Async Type-Safe):**
```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver<Video>(async video =>
    {
        var metadata = await _videoService.GetMetadataAsync(video.Elements.VideoId);
        return $"<div class=\"video-embed\">" +
               $"<iframe src=\"https://youtube.com/embed/{video.Elements.VideoId}\" " +
               $"title=\"{metadata.Title}\"></iframe></div>";
    })
    .Build();
```

**New (Codename-Based):**
```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver("tweet", content =>
    {
        if (content is IEmbeddedContent<Tweet> tweet)
        {
            return $"<blockquote>{tweet.Elements.TweetText}</blockquote>";
        }
        return string.Empty;
    })
    .Build();
```

### 5.3 Batch Resolver Registration

**New:**
```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolvers(
        (typeof(Tweet), content =>
            content is IEmbeddedContent<Tweet> t
                ? $"<blockquote>{t.Elements.TweetText}</blockquote>"
                : ""),
        (typeof(Video), content =>
            content is IEmbeddedContent<Video> v
                ? $"<iframe src=\"https://youtube.com/embed/{v.Elements.VideoId}\"></iframe>"
                : ""),
        (typeof(Quote), content =>
            content is IEmbeddedContent<Quote> q
                ? $"<blockquote><p>{q.Elements.Text}</p><cite>{q.Elements.Author}</cite></blockquote>"
                : ""))
    .Build();
```

### 5.4 URL Pattern Resolver

**New (simplified link resolution):**
```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver(DefaultResolvers.UrlPatternResolver(
        new Dictionary<string, string>
        {
            ["article"] = "/articles/{urlslug}",
            ["product"] = "/shop/products/{urlslug}",
            ["category"] = "/categories/{codename}"
        },
        fallbackPattern: "/content/{id}"))
    .Build();
```

### 5.5 Inline Image Resolution

**Legacy:**
```csharp
// Images were typically handled automatically or via MVC display templates
```

**New:**
```csharp
var resolver = new HtmlResolverBuilder()
    .WithInlineImageResolver((image, resolveChildren) =>
    {
        var alt = !string.IsNullOrEmpty(image.Description)
            ? image.Description
            : "image description";
        return ValueTask.FromResult(
            $"<figure><img src=\"{image.Url}\" alt=\"{alt}\" />" +
            $"<figcaption>{alt}</figcaption></figure>");
    })
    .Build();
```

### 5.6 HTML Node Resolution

**New (customize specific HTML elements):**
```csharp
var resolver = new HtmlResolverBuilder()
    // Customize all h1 elements
    .WithHtmlNodeResolver("h1", async (node, resolveChildren) =>
    {
        var content = await resolveChildren(node.Children);
        return $"<h1 class=\"page-heading\">{content}</h1>";
    })
    // Add anchors to headings
    .WithHtmlNodeResolver("h2", async (node, resolveChildren) =>
    {
        var content = await resolveChildren(node.Children);
        var id = GenerateSlug(content);
        return $"<h2 id=\"{id}\"><a href=\"#{id}\">{content}</a></h2>";
    })
    .Build();
```

### 5.7 HTML to String Conversion

**Legacy:**
```csharp
// Rich text was a string property
string bodyHtml = article.BodyCopy;
```

**New:**
```csharp
// Rich text is now a RichTextContent type that needs resolution
var html = await article.BodyCopy.ToHtmlAsync(resolver);

// Or with default resolution (no custom resolvers)
var html = await article.BodyCopy.ToHtmlAsync();
```

### 5.8 Accessing Rich Text Content Programmatically

**New:**
```csharp
// Get inline images
var images = article.BodyCopy.GetInlineImages().ToList();

// Get content item links
var links = article.BodyCopy.GetContentItemLinks().ToList();

// Get embedded content of specific type
var tweets = article.BodyCopy.GetEmbeddedContent<Tweet>().ToList();

// Get just the element models (without wrapper)
var tweetElements = article.BodyCopy.GetEmbeddedElements<Tweet>().ToList();

// Iterate over all blocks
foreach (var block in article.BodyCopy)
{
    switch (block)
    {
        case IEmbeddedContent<Tweet> tweet:
            Console.WriteLine($"Tweet: {tweet.Elements.TweetText}");
            break;
        case IInlineImage image:
            Console.WriteLine($"Image: {image.Url}");
            break;
        case IHtmlNode htmlNode:
            Console.WriteLine($"HTML: {htmlNode.TagName}");
            break;
    }
}
```

### 5.9 Removed: MVC Display Templates

**Legacy (no longer supported):**
```
Views/
└── Shared/
    └── DisplayTemplates/
        ├── Tweet.cshtml
        ├── Video.cshtml
        └── InlineImage.cshtml
```

**Migration:** Use `HtmlResolverBuilder.WithContentResolver<T>()` instead. Display templates are no longer automatically discovered or used.

### 5.10 Other `HtmlResolverBuilder` extension points

For completeness, the builder also exposes these less commonly used hooks:

- `WithTextNodeResolver(BlockResolver<ITextNode>)` — override how plain text nodes are emitted (default HTML-encodes).
- `WithHtmlElementResolver(BlockResolver<IHtmlNode>)` — override the *default* HTML node resolver (used when no specific tag/attribute predicate matches).
- `WithHtmlNodeResolver(HtmlNodePredicate, BlockResolver<IHtmlNode>, string description = null)` — predicate-based HTML node resolver with an optional diagnostic description.
- `WithHtmlNodeResolverForAttribute(string attributeName, string? attributeValue, BlockResolver<IHtmlNode>)` — match HTML nodes by attribute name/value.
- `WithContentItemLinkResolvers(...)` and `WithContentResolvers(...)` batch overloads — register many resolvers in one call (dictionary or `params (codename, resolver)[]`).
- `ThrowOnMissingResolver(bool enabled = true)` — fail fast when a `WithContentResolver` / `WithContentItemLinkResolver` is missing for an embedded item or content link encountered during rendering.

---

## 6. Migrating Response Handling

The SDK now uses a result pattern instead of throwing exceptions for API errors.

### 6.1 Result Pattern

**Legacy:**
```csharp
try
{
    var response = await client.GetItemAsync<Article>("article_codename");
    var title = response.Item.Title;
}
catch (DeliveryException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

**New:**
```csharp
var result = await client.GetItem<Article>("article_codename").ExecuteAsync();

if (result.IsSuccess)
{
    var title = result.Value.Elements.Title;
}
else
{
    var error = result.Error;
    Console.WriteLine($"Error: {error.Message}");
    Console.WriteLine($"Status: {result.StatusCode}");

    if (error.RequestId != null)
        Console.WriteLine($"Request ID: {error.RequestId}");

    if (error.ErrorCode.HasValue)
        Console.WriteLine($"Error Code: {error.ErrorCode}");
}
```

### 6.2 Accessing Content

**Legacy:**
```csharp
var response = await client.GetItemAsync<Article>("article_codename");

// Direct property access
var title = response.Item.Title;
var summary = response.Item.Summary;
var systemName = response.Item.System.Name;
```

**New:**
```csharp
var result = await client.GetItem<Article>("article_codename").ExecuteAsync();

if (result.IsSuccess)
{
    // Content is accessed via Elements property
    var title = result.Value.Elements.Title;
    var summary = result.Value.Elements.Summary;

    // System properties are on the wrapper
    var systemName = result.Value.System.Name;
    var codename = result.Value.System.Codename;
    var language = result.Value.System.Language;
}
```

### 6.3 Accessing Items Collection

**Legacy:**
```csharp
var response = await client.GetItemsAsync<Article>();

foreach (var article in response.Items)
{
    Console.WriteLine(article.Title);
}

// Pagination info
var totalCount = response.Pagination.TotalCount;
```

**New:**
```csharp
var result = await client.GetItems<Article>()
    .WithTotalCount()
    .ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var article in result.Value.Items)
    {
        Console.WriteLine(article.Elements.Title);
    }

    // Pagination info
    var totalCount = result.Value.Pagination.TotalCount;
}
```

### 6.4 IError Properties

| Property | Description |
|----------|-------------|
| `Message` | Human-readable error description |
| `RequestId` | Unique request ID for Kontent.ai support |
| `ErrorCode` | Kontent.ai-specific error code |
| `SpecificCode` | More specific error code |
| `Exception` | Underlying exception (for network errors) |

### 6.5 Response Metadata

**Properties on `IDeliveryResult<T>`:**

| Property | Description |
|----------|-------------|
| `IsSuccess` | Whether the request succeeded (HTTP 2xx). |
| `Value` | The response content. Populated when `IsSuccess` is `true`. |
| `Error` | `IError` with diagnostic details. Populated when `IsSuccess` is `false`. |
| `StatusCode` | HTTP status code. |
| `RequestUrl` | Full request URL for debugging. `null` when served from the SDK cache. |
| `ResponseHeaders` | HTTP response headers. `null` when served from the SDK cache. |
| `ResponseSource` | `Origin`, `Cdn`, `Cache`, or `FailSafe` — explains *where* the response came from. |
| `IsCacheHit` | `true` when `ResponseSource` is `Cache` or `FailSafe` (i.e., the SDK served it without calling the API). |
| `HasStaleContent` | `true` when Kontent.ai signalled that newer content may be available shortly (rolling cache window). |
| `DependencyKeys` | List of cache dependency keys (e.g., `item_<codename>`, `type_<codename>`, list scopes) associated with this response. Use these to drive webhook-based invalidation via `IDeliveryCacheManager.InvalidateAsync(...)`. |

**Example:**
```csharp
var result = await client.GetItem<Article>("article_codename").ExecuteAsync();

Console.WriteLine($"Success: {result.IsSuccess}");
Console.WriteLine($"Status: {result.StatusCode}");
Console.WriteLine($"Source: {result.ResponseSource}");      // Origin | Cdn | Cache | FailSafe
Console.WriteLine($"Request URL: {result.RequestUrl}");

if (!result.IsCacheHit && result.ResponseHeaders is not null)
{
    if (result.ResponseHeaders.TryGetValues("X-Cache", out var cacheValues))
    {
        Console.WriteLine($"CDN Cache: {string.Join(", ", cacheValues)}");
    }
}

// DependencyKeys feed straight into webhook invalidation
if (result.DependencyKeys is { Count: > 0 } keys)
{
    Console.WriteLine($"Tracked dependency keys: {string.Join(", ", keys)}");
}
```

---

## 7. Migrating Model Structure

The SDK uses a different model structure with `IContentItem<T>` wrappers.

### 7.1 Model Generator Update

**Legacy (model generator 9.x or earlier):**
```bash
KontentModelGenerator --environmentid <id> --outputdir Models
```

**New (model generator 10.0.0 required):**
```bash
dotnet tool install -g Kontent.Ai.ModelGenerator --version 10.0.0
KontentModelGenerator --environmentid <id> --outputdir Models
```

### 7.2 Model Structure Changes

**Legacy model:**
```csharp
public class Article
{
    public string Title { get; set; }
    public string Summary { get; set; }
    public string BodyCopy { get; set; }
    public DateTime? PostDate { get; set; }
    public IEnumerable<object> RelatedArticles { get; set; }
    public ContentItemSystemAttributes System { get; set; }
}
```

**New model:**
```csharp
public record Article
{
    [JsonPropertyName("title")]
    public string Title { get; init; }

    [JsonPropertyName("summary")]
    public string Summary { get; init; }

    [JsonPropertyName("body_copy")]
    public RichTextContent BodyCopy { get; init; }

    [JsonPropertyName("post_date")]
    public DateTime? PostDate { get; init; }

    [JsonPropertyName("related_articles")]
    public IEnumerable<IEmbeddedContent>? RelatedArticles { get; init; }
}
```

**Key differences:**
- `System` property is no longer on the model - it's on the `IContentItem<T>` wrapper
- Rich text is `RichTextContent` instead of `string`
- Linked items are `IEnumerable<IEmbeddedContent>` instead of `IEnumerable<object>`
- Properties use `init` setters for immutability

### 7.3 Property Access Changes

**Legacy:**
```csharp
var response = await client.GetItemAsync<Article>("article");
var title = response.Item.Title;
var systemName = response.Item.System.Name;
```

**New:**
```csharp
var result = await client.GetItem<Article>("article").ExecuteAsync();

if (result.IsSuccess)
{
    // Element properties are accessed via .Elements
    var title = result.Value.Elements.Title;

    // System properties are on the wrapper, not the model
    var systemName = result.Value.System.Name;
    var codename = result.Value.System.Codename;
    var type = result.Value.System.Type;
}
```

### 7.4 Linked Items / Modular Content

**Legacy:**
```csharp
var response = await client.GetItemAsync<Article>("article");

foreach (var related in response.Item.RelatedArticles)
{
    if (related is Article relatedArticle)
    {
        Console.WriteLine(relatedArticle.Title);
    }
}
```

**New:**
```csharp
var result = await client.GetItem<Article>("article").ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var related in result.Value.Elements.RelatedArticles!)
    {
        // Pattern match to get strongly-typed content
        if (related is IEmbeddedContent<Article> relatedArticle)
        {
            // Access system properties
            Console.WriteLine($"Codename: {relatedArticle.System.Codename}");

            // Access element properties
            Console.WriteLine($"Title: {relatedArticle.Elements.Title}");
        }
    }
}
```

### 7.5 Filtering Linked Items by Type

**New:**
```csharp
// Get only articles from mixed linked items
var articles = result.Value.Elements.RelatedArticles!
    .OfType<IEmbeddedContent<Article>>()
    .ToList();

foreach (var article in articles)
{
    Console.WriteLine(article.Elements.Title);
}

// Extract just the element models (without wrapper)
var articleElements = result.Value.Elements.RelatedArticles!
    .OfType<IEmbeddedContent<Article>>()
    .Select(a => a.Elements)
    .ToList();
```

### 7.6 Mixed Content Types

**New:**
```csharp
foreach (var item in homepage.Elements.FeaturedContent!)
{
    switch (item)
    {
        case IEmbeddedContent<Article> article:
            RenderArticleCard(article.Elements);
            break;
        case IEmbeddedContent<Product> product:
            RenderProductCard(product.Elements);
            break;
        case IEmbeddedContent<Video> video:
            RenderVideoEmbed(video.Elements);
            break;
        default:
            Console.WriteLine($"Unknown type: {item.System.Type}");
            break;
    }
}
```

---

## 8. Migrating DI Registration

### 8.1 Basic Registration

**Legacy (unchanged pattern):**
```csharp
services.AddDeliveryClient(Configuration);
```

**New (same, but also supports new overloads):**
```csharp
// From configuration
services.AddDeliveryClient(configuration, "DeliveryOptions");

// From action
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});

// From builder
services.AddDeliveryClient(builder =>
    builder.WithEnvironmentId("your-environment-id")
           .UseProductionApi()
           .Build());
```

### 8.2 Custom Services Registration

**Legacy:**
```csharp
services.AddSingleton<IContentLinkUrlResolver, CustomContentLinkUrlResolver>();
services.AddSingleton<IInlineContentItemsResolver<Tweet>, TweetResolver>();
services.AddSingleton<ITypeProvider, CustomTypeProvider>();
services.AddSingleton<IRetryPolicyProvider, CustomRetryPolicyProvider>();
services.AddDeliveryClient(Configuration);
```

**New:**
```csharp
// Optional: register a CUSTOM ITypeProvider BEFORE AddDeliveryClient().
// AddDeliveryClient uses TryAddSingleton, so your registration wins only if it's already in the container.
// You do NOT need to register the source-generated provider manually — the SDK auto-discovers it
// from loaded assemblies via the [ContentTypeCodename] attribute.
services.AddSingleton<ITypeProvider, MyCustomTypeProvider>();

// Then register the delivery client
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});

// Rich text resolution is now done at use site via HtmlResolverBuilder
// (no global registration)

// Retry policies via configureResilience callback
services.AddDeliveryClient(
    buildDeliveryOptions: builder => builder.WithEnvironmentId("env-id").Build(),
    configureResilience: builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions { /* ... */ });
    });
```

> [!IMPORTANT]
> Order matters only when **overriding** `ITypeProvider`. The SDK registers the default `TypeProvider` (which auto-discovers source-generated providers) via `TryAddSingleton`, so a manual `AddSingleton` registered *before* `AddDeliveryClient` will replace it.

### 8.3 Named Clients

**Legacy:**
```csharp
services.AddDeliveryClient("production", Configuration, "ProductionDeliveryOptions");
services.AddDeliveryClient("preview", Configuration, "PreviewDeliveryOptions");

// Get client
var factory = serviceProvider.GetRequiredService<IDeliveryClientFactory>();
var client = factory.Get("production");
```

**New:**
```csharp
services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "production-env-id";
});
services.AddDeliveryClient("preview", options =>
{
    options.EnvironmentId = "preview-env-id";
    options.UsePreviewApi = true;
    options.PreviewApiKey = "preview-api-key";
});

// Get client via factory (same as before)
var factory = serviceProvider.GetRequiredService<IDeliveryClientFactory>();
var client = factory.Get("production");

// Or use keyed services (new .NET 8+ feature)
public class MyController(
    [FromKeyedServices("production")] IDeliveryClient productionClient,
    [FromKeyedServices("preview")] IDeliveryClient previewClient)
{
}
```

### 8.4 HttpClient Configuration

**Legacy:** v18 didn't expose a public hook for configuring the underlying `HttpClient` — `IDeliveryHttpClient` was registered as a singleton internally and not surfaced as a typed `HttpClient`. Customizing the timeout typically required replacing `IDeliveryHttpClient` wholesale.

**New:** Pass a `configureHttpClient` callback to `AddDeliveryClient`.
```csharp
services.AddDeliveryClient(
    buildDeliveryOptions: builder => builder.WithEnvironmentId("env-id").Build(),
    configureHttpClient: builder =>
    {
        builder.ConfigureHttpClient(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
    });
```

The callback receives the `IHttpClientBuilder` for the SDK's named `HttpClient`, so you can also chain `.AddHttpMessageHandler(...)`, attach Polly handlers, etc.

### 8.5 DeliveryClientBuilder (Non-DI)

**Legacy:**
```csharp
var client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithEnvironmentId("env-id")
        .UseProductionApi()
        .Build())
    .WithContentLinkUrlResolver(new CustomResolver())
    .WithTypeProvider(new CustomTypeProvider())
    .Build();
```

**New:**
```csharp
await using var client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithEnvironmentId("env-id")
        .UseProductionApi()
        .Build())
    .WithTypeProvider(new GeneratedTypeProvider())
    .WithLoggerFactory(loggerFactory)            // optional: enable diagnostic logging
    .WithMemoryCache(TimeSpan.FromHours(1))      // from Kontent.Ai.Delivery.Caching
    .ConfigureServices(services =>               // general-purpose extensibility hook
    {
        services.AddSingleton<IMyCustomDependency, MyImpl>();
    })
    .Build();

// Note: Content link resolution is now done at use site via HtmlResolverBuilder
```

The new builder surface is intentionally minimal:

| Method | Purpose |
|--------|---------|
| `WithOptions(Func<IDeliveryOptionsBuilder, DeliveryOptions>)` | Required. Configures `DeliveryOptions`. |
| `WithTypeProvider(ITypeProvider)` | Override the default type provider. |
| `WithLoggerFactory(ILoggerFactory)` | Enable logging (no-op if omitted). |
| `WithMemoryCache(...)` / `WithHybridCache(...)` | From `Kontent.Ai.Delivery.Caching`. |
| `ConfigureServices(Action<IServiceCollection>)` | Escape hatch — register anything else into the builder's internal service collection. |
| `Build()` | Returns an `IDeliveryClient` that owns its services; dispose via `await using`. |

Anything not listed above (e.g., legacy `WithContentLinkUrlResolver`, `WithRetryPolicyProvider`, `WithInlineContentItemsResolver<T>`, `WithDeliveryHttpClient`, `WithModelProvider`, `WithInlineContentItemsProcessor`, `WithPropertyMapper`) was removed — see [§9](#9-removed-features--interfaces) for replacements.

---

## 9. Removed Features & Interfaces

### 9.1 Removed Interfaces

| Removed Interface | Replacement |
|------------------|-------------|
| `IContentLinkUrlResolver` | `HtmlResolverBuilder.WithContentItemLinkResolver()` |
| `IInlineContentItemsResolver<T>` | `HtmlResolverBuilder.WithContentResolver<T>()` |
| `IInlineContentItemsProcessor` | `HtmlResolverBuilder` + `IRichTextContent.ToHtmlAsync(resolver)` |
| `IRetryPolicy` | Polly `ResiliencePipelineBuilder<HttpResponseMessage>` via `configureResilience` |
| `IRetryPolicyProvider` | Polly `ResiliencePipelineBuilder<HttpResponseMessage>` via `configureResilience` |
| `IDeliveryHttpClient` | Configure the underlying `HttpClient` via the `configureHttpClient` callback on `AddDeliveryClient` (see [§8.4](#84-httpclient-configuration)). |
| `IDeliverySyncInitResponse`, `IDeliverySyncResponse`, `IDeliverySyncV2Response` | Sync was extracted to the standalone [`Kontent.Ai.Sync`](https://github.com/kontent-ai/sync-sdk-net) package — see [§11](#11-migrating-the-sync-api). |

### 9.2 Removed Classes

| Removed Class | Replacement |
|--------------|-------------|
| `DefaultRetryPolicyOptions` | `HttpRetryStrategyOptions` from Polly |
| `DeliveryCacheOptions` (legacy shape with `CacheType`/`StaleContentExpiration`) | Replaced by a new, FusionCache-shaped `DeliveryCacheOptions` exposed via `Action<DeliveryCacheOptions>` overloads of `AddDeliveryMemoryCache()` / `AddDeliveryHybridCache()` (see [§3.2](#32-memory-cache-registration) / [§3.3](#33-hybrid-cache-registration-redis)). |
| `DeliveryClientCache` | Internal implementation (use `AddDelivery*Cache()` methods) |
| `CacheManagerFactory` | Use `DeliveryClientBuilder.WithMemoryCache()` or `WithHybridCache()` |
| `EqualsFilter`, `NotEqualsFilter`, `LessThanFilter`, `LessThanOrEqualFilter`, `GreaterThanFilter`, `GreaterThanOrEqualFilter`, `RangeFilter`, `InFilter`, `NotInFilter`, `ContainsFilter`, `AnyFilter`, `AllFilter`, `EmptyFilter`, `NotEmptyFilter`, `SystemTypeEqualsFilter` | Fluent filter builder via `.Where(f => f.System(...).Is...() / f.Element(...).Is...())` |
| `LanguageParameter`, `LimitParameter`, `SkipParameter`, `OrderParameter`, `DepthParameter`, `ElementsParameter`, `ExcludeElementsParameter`, `IncludeTotalCountParameter`, `ContinuationTokenParameter` | Fluent query builder methods (`.WithLanguage`, `.Limit`, `.Skip`, `.OrderBy`, `.Depth`, `.WithElements`, `.WithoutElements`, `.WithTotalCount`, automatic feed pagination) |
| `SortOrder` enum | Replaced by `OrderingMode` (`Ascending` / `Descending`) used with `.OrderBy(...)`. |

### 9.3 Removed Methods

| Removed Method | Replacement |
|---------------|-------------|
| `GetItemAsync<T>(codename, params)` | `GetItem<T>(codename).[options].ExecuteAsync()` |
| `GetItemsAsync<T>(params)` | `GetItems<T>().[options].ExecuteAsync()` |
| `GetItemsFeed<T>(params)` (legacy returned `IDeliveryItemsFeed<T>` immediately) | `GetItemsFeed<T>().[options].EnumerateAsync()` (or `.ExecuteAsync()` for first page) |
| `GetTypeAsync(codename)` | `GetType(codename).ExecuteAsync()` |
| `GetTypesAsync(params)` | `GetTypes().[options].ExecuteAsync()` |
| `GetTaxonomyAsync(codename)` | `GetTaxonomy(codename).ExecuteAsync()` |
| `GetTaxonomiesAsync(params)` | `GetTaxonomies().[options].ExecuteAsync()` |
| `GetLanguagesAsync()` | `GetLanguages().ExecuteAsync()` |
| `GetContentElementAsync(type, element)` | `GetContentElement(type, element).ExecuteAsync()` |
| `PostSyncInitAsync(params)`, `GetSyncAsync(token)`, `PostSyncV2InitAsync()`, `GetSyncV2Async(token)` | Sync API moved to the standalone [`Kontent.Ai.Sync`](https://github.com/kontent-ai/sync-sdk-net) package — see [§11](#11-migrating-the-sync-api). |
| `services.AddDeliveryInlineContentItemsResolver<T>(...)` (and the two-generic-type overload) | Use `HtmlResolverBuilder.WithContentResolver<T>(...)` at use site. |
| `DeliveryClientBuilder.WithContentLinkUrlResolver()` | Use `HtmlResolverBuilder` at use site |
| `DeliveryClientBuilder.WithRetryPolicyProvider()` | Use `configureResilience` callback |
| `DeliveryClientBuilder.WithInlineContentItemsResolver<T>()` | Use `HtmlResolverBuilder` at use site |
| `DeliveryClientBuilder.WithInlineContentItemsProcessor()` | Use `HtmlResolverBuilder` at use site |
| `DeliveryClientBuilder.WithDeliveryHttpClient()` | Use `configureHttpClient` callback on `AddDeliveryClient` (DI) — there is no equivalent on the non-DI builder. |
| `DeliveryClientBuilder.WithModelProvider()` | Removed — model materialization is internal in 19.0. |
| `DeliveryClientBuilder.WithPropertyMapper()` | Removed — element-to-property mapping is internal in 19.0. |

### 9.4 Removed DeliveryOptions Properties

| Removed Property | Replacement |
|-----------------|-------------|
| `EnableRetryPolicy` | `DisableRetryPolicy()` on the options builder (sets the new `EnableResilience = false` internally). |
| `DefaultRetryPolicyOptions` | `configureResilience` callback on `AddDeliveryClient` with Polly's `HttpRetryStrategyOptions`. |
| `IncludeTotalCount` | Per-query `.WithTotalCount()` on items / dynamic items queries (no global default any more). |
| `WaitForLoadingNewContent` | Per-query `.WaitForLoadingNewContent(true)` on every query builder (also bypasses the SDK cache for that single call). |

### 9.5 Removed Packages

| Removed Package | Replacement |
|----------------|-------------|
| `Kontent.Ai.Delivery.Caching` (18.x) | `Kontent.Ai.Delivery.Caching` (19.x) — new FusionCache-backed implementation with different API |
| `Kontent.Ai.Delivery.Rx` | Use `IAsyncEnumerable<T>` with `await foreach` instead |

**Reactive Extensions Migration:**

The `Kontent.Ai.Delivery.Rx` package has been removed. Reactive/Observable patterns are replaced with `IAsyncEnumerable<T>`:

**Legacy:**
```csharp
using Kontent.Ai.Delivery.Rx;

// Subscribe to items as observable
client.GetItemsObservable<Article>()
    .Subscribe(
        onNext: article => Console.WriteLine(article.Title),
        onError: ex => Console.WriteLine($"Error: {ex}"),
        onCompleted: () => Console.WriteLine("Done"));
```

**New:**
```csharp
// Use native async enumeration
await foreach (var article in client.GetItemsFeed<Article>().EnumerateAsync())
{
    Console.WriteLine(article.Elements.Title);
}

// For reactive patterns, wrap with System.Reactive if needed:
// Install-Package System.Reactive
using System.Reactive.Linq;

var observable = client.GetItemsFeed<Article>()
    .EnumerateAsync()
    .ToObservable();

observable.Subscribe(
    onNext: article => Console.WriteLine(article.Elements.Title),
    onError: ex => Console.WriteLine($"Error: {ex}"),
    onCompleted: () => Console.WriteLine("Done"));
```

---

## 10. New Features

These features are optional to adopt but provide improved functionality.

### 10.1 Async Enumeration

Efficiently iterate over large datasets without loading all items into memory:

```csharp
await foreach (var item in client.GetItemsFeed<Article>().EnumerateAsync())
{
    await ProcessItemAsync(item);
}
```

### 10.2 Automatic Pagination

Navigate through paginated results without manual token handling:

```csharp
var result = await client.GetItems<Article>()
    .Limit(10)
    .WithTotalCount()
    .ExecuteAsync();

if (result.IsSuccess)
{
    // Process first page
    ProcessItems(result.Value.Items);

    // Check and fetch next pages
    while (result.Value.HasNextPage)
    {
        result = await result.Value.FetchNextPageAsync();
        if (result?.IsSuccess == true)
        {
            ProcessItems(result.Value.Items);
        }
    }
}
```

### 10.3 Cache Hit Detection

Monitor cache effectiveness:

```csharp
var result = await client.GetItem<Article>("homepage").ExecuteAsync();

if (result.IsCacheHit)
{
    _metrics.IncrementCacheHits();
}
else
{
    _metrics.IncrementCacheMisses();
}
```

### 10.4 Response Metadata

Access request details for debugging and monitoring:

```csharp
var result = await client.GetItems<Article>().ExecuteAsync();

_logger.LogInformation("Request: {Url}, Status: {Status}, CacheHit: {CacheHit}",
    result.RequestUrl,
    result.StatusCode,
    result.IsCacheHit);

if (result.HasStaleContent)
{
    _logger.LogWarning("Content may be stale - consider refreshing");
}
```

### 10.5 Language Fallback Control

Disable language fallbacks for list/feed queries:

```csharp
// Allow fallbacks (default behavior)
var result = await client.GetItems<Article>()
    .WithLanguage("es-ES")
    .ExecuteAsync();

// Disable fallbacks - only return items actually translated to Spanish
var result = await client.GetItems<Article>()
    .WithLanguage("es-ES", LanguageFallbackMode.Disabled)
    .ExecuteAsync();
```

> Single-item queries (`GetItem<T>(...)` and dynamic `GetItem(...)`) do not support `LanguageFallbackMode.Disabled`. They always use `language=<lang>` and follow configured fallback behavior.

### 10.6 Reference Lookups (Used In)

Find content that references specific items or assets:

```csharp
// Find items referencing a content item
await foreach (var usage in client.GetItemUsedIn("author_john").EnumerateAsync())
{
    Console.WriteLine($"Referenced by: {usage.System.Name}");
}

// Find items using a specific asset
await foreach (var usage in client.GetAssetUsedIn("hero_image").EnumerateAsync())
{
    Console.WriteLine($"Asset used in: {usage.System.Name}");
}
```

`EnumerateAsync()` stops when a subsequent page request fails and returns the items already received.
If you need explicit page-level failure handling, use `EnumerateItemsWithStatusAsync()`:

```csharp
await foreach (var page in client.GetAssetUsedIn("hero_image").EnumerateItemsWithStatusAsync())
{
    if (!page.IsSuccess)
    {
        Console.WriteLine($"Lookup failed with {(int)page.StatusCode}: {page.Error?.Message}");
        break;
    }

    foreach (var usage in page.Value)
    {
        Console.WriteLine($"Asset used in: {usage.System.Name}");
    }
}
```

The same status-aware helper is available for feed queries:

```csharp
await foreach (var page in client.GetItemsFeed<Article>().EnumerateItemsWithStatusAsync())
{
    if (!page.IsSuccess)
    {
        Console.WriteLine($"Feed failed with {(int)page.StatusCode}: {page.Error?.Message}");
        break;
    }

    foreach (var item in page.Value.Items)
    {
        ProcessItem(item);
    }
}
```

### 10.7 Element Projection

Reduce response size by selecting specific elements:

```csharp
// Include only specific elements
var result = await client.GetItems<Article>()
    .WithElements("title", "summary", "url_slug")
    .ExecuteAsync();

// Exclude specific elements
var result = await client.GetItems<Article>()
    .WithoutElements("body_copy", "metadata")
    .ExecuteAsync();
```

### 10.8 ResponseSource & DependencyKeys

Every result now carries a `ResponseSource` enum (`Origin`, `Cdn`, `Cache`, `FailSafe`) and a list of `DependencyKeys`. The enum lets you distinguish a normal cache hit from a fail-safe stale serve, and the dependency keys plug straight into webhook-driven invalidation:

```csharp
var result = await client.GetItems<Article>().ExecuteAsync();

// Persist these alongside your CDN entry, then call InvalidateAsync(...) when a webhook fires.
var keysForInvalidation = result.DependencyKeys;
```

See [§3.6](#36-cache-invalidation) for the full webhook flow and [§6.5](#65-response-metadata) for the complete `IDeliveryResult` surface.

### 10.9 Configurable Fail-Safe and Eager Refresh

The new `DeliveryCacheOptions` exposes FusionCache's fail-safe (serve stale on API failure) and eager-refresh (refresh in the background before TTL expires) features. See [§3.5](#35-deliverycacheoptions-migration) for the full set of knobs.

---

## 11. Migrating the Sync API

The Delivery Sync API methods (`PostSyncInitAsync`, `GetSyncAsync`, `PostSyncV2InitAsync`, `GetSyncV2Async`) have been **removed** from `IDeliveryClient` and moved to a dedicated package: [`Kontent.Ai.Sync`](https://github.com/kontent-ai/sync-sdk-net). This isolates sync-specific concerns (continuation token lifecycle, delta payloads, v2 multi-resource sync) from the general delivery client.

### 11.1 Package Change

```xml
<!-- Before (everything bundled in the delivery client) -->
<PackageReference Include="Kontent.Ai.Delivery" Version="18.x.x" />

<!-- After: add the standalone sync package alongside the delivery client -->
<PackageReference Include="Kontent.Ai.Delivery" Version="19.0.0" />
<PackageReference Include="Kontent.Ai.Sync" Version="*" />
```

### 11.2 Side-by-side example

**Legacy (init + page through deltas with the same `IDeliveryClient`):**
```csharp
var initResponse = await deliveryClient.PostSyncInitAsync();
var continuationToken = initResponse.ContinuationToken;

while (!string.IsNullOrEmpty(continuationToken))
{
    var sync = await deliveryClient.GetSyncAsync(continuationToken);

    foreach (var change in sync.SyncItems)
    {
        ProcessChange(change);
    }

    continuationToken = sync.ContinuationToken;
}
```

**New (use the standalone Sync SDK):**
```csharp
// Registration
services.AddSyncClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});

// Usage
var syncClient = serviceProvider.GetRequiredService<ISyncClient>();

// Initialize and get the starting sync token
var init = await syncClient.InitializeSyncAsync();
var syncToken = init.Value;

// Pull a single delta page
var page = await syncClient.GetDeltaAsync(syncToken);

// Or stream all available delta pages (capped by maxPages to bound work per call)
await foreach (var delta in syncClient.GetAllDeltaAsync(syncToken, maxPages: 10))
{
    foreach (var change in delta.Items)
    {
        ProcessChange(change);
    }
}
```

> [!NOTE]
> The Sync SDK is a separate project with its own release cadence. See [`kontent-ai/sync-sdk-net`](https://github.com/kontent-ai/sync-sdk-net) for the authoritative API reference, supported features (item / type / taxonomy / language deltas), and full DI registration patterns.

---

## Troubleshooting

### Common Migration Issues

#### "Cannot implicitly convert type" errors

**Problem:** Code using `response.Item.Title` fails to compile.

**Solution:** Update to use the new result pattern:
```csharp
// Before
var title = response.Item.Title;

// After
var title = result.Value.Elements.Title;
```

#### "The type or namespace 'EqualsFilter' could not be found"

**Problem:** Filter parameter classes no longer exist.

**Solution:** Use the fluent filter builder:
```csharp
// Before
new EqualsFilter("system.type", "article")

// After
.Where(f => f.System("type").IsEqualTo("article"))
```

#### "IContentLinkUrlResolver does not exist"

**Problem:** Rich text resolver interfaces have been removed.

**Solution:** Use `HtmlResolverBuilder` at the point of use:
```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver("article", async (link, resolveChildren) =>
    {
        var innerHtml = await resolveChildren(link.Children);
        return $"<a href=\"/articles/{link.Metadata?.UrlSlug}\">{innerHtml}</a>";
    })
    .Build();

var html = await richText.ToHtmlAsync(resolver);
```

#### "AddDeliveryClientCache does not exist"

**Problem:** The legacy caching API has been replaced.

**Solution:** Update your `Kontent.Ai.Delivery.Caching` package to 19.0.0+ and use the new registration methods:
```csharp
// Before
services.AddDeliveryClientCache(new DeliveryCacheOptions { CacheType = CacheTypeEnum.Memory });

// After (requires Kontent.Ai.Delivery.Caching 19.0.0+)
services.AddDeliveryMemoryCache(defaultExpiration: TimeSpan.FromHours(1));
```

#### "Cannot resolve IDeliveryClient"

**Problem:** Named clients may need explicit resolution.

**Solution:** Use keyed services or the factory:
```csharp
// Via factory
var factory = serviceProvider.GetRequiredService<IDeliveryClientFactory>();
var client = factory.Get("clientName");

// Via keyed services (controller)
public class MyController([FromKeyedServices("clientName")] IDeliveryClient client)
```

#### Model properties return null unexpectedly

**Problem:** Models from old generator don't work with new SDK.

**Solution:** Regenerate models with model generator 10.0.0:
```bash
dotnet tool update -g Kontent.Ai.ModelGenerator --version 10.0.0
KontentModelGenerator --environmentid <id> --outputdir Models --force
```

### FAQ

**Q: Can I use old models with the new SDK?**

A: No. The new SDK requires models generated with Kontent.Ai.ModelGenerator 10.0.0. The model structure has changed significantly (e.g., `System` property location, linked items typing).

**Q: How do I migrate custom retry policies?**

A: Implement your retry logic using Polly's `HttpRetryStrategyOptions` in the `configureResilience` callback. See [Section 4.5](#45-custom-retry-policy-migration) for detailed examples.

**Q: What happened to `IDeliveryCacheManager.InvalidateEntry()`?**

A: Use `IDeliveryCacheManager.InvalidateAsync()` with dependency keys instead. The key format is `item_{codename}` for items, `asset_{id}` for assets, `type_{codename}` for content types, and `taxonomy_{group}` for taxonomies. For broad listing invalidation, use `DeliveryCacheDependencies.ItemsListScope` (`scope_items_list`), `DeliveryCacheDependencies.TypesListScope` (`scope_types_list`), and `DeliveryCacheDependencies.TaxonomiesListScope` (`scope_taxonomies_list`).

**Q: Why is my rich text empty after migration?**

A: Rich text is now a `RichTextContent` type that must be resolved using `ToHtmlAsync()`. It's no longer a simple string:
```csharp
var html = await article.BodyCopy.ToHtmlAsync(resolver);
```

**Q: How do I access System properties on linked items?**

A: Linked items are now `IEmbeddedContent<T>` which has both `System` and `Elements` properties:
```csharp
foreach (var item in result.Value.Elements.RelatedArticles!)
{
    if (item is IEmbeddedContent<Article> article)
    {
        Console.WriteLine(article.System.Codename);  // System properties
        Console.WriteLine(article.Elements.Title);   // Element properties
    }
}
```

**Q: Is there a way to get the raw response like before?**

A: The result object provides `RequestUrl`, `ResponseHeaders`, and `StatusCode` for debugging purposes. For raw JSON, you would need to make requests directly using `HttpClient`.
