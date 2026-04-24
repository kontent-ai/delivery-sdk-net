# For Developers: SDK Internal Architecture

This document provides a comprehensive overview of the Kontent.ai Delivery .NET SDK's internal architecture for maintainers and contributors. It covers the implementation patterns, architectural decisions, and key components that make up the SDK.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Refit API Layer](#refit-api-layer)
- [Filtering (Fluent DSL)](#filtering-fluent-dsl)
- [HTTP Pipeline & Delegating Handlers](#http-pipeline--delegating-handlers)
- [Caching Architecture](#caching-architecture)
- [Rich Text Resolution System](#rich-text-resolution-system)
- [Builder Patterns](#builder-patterns)
- [Service Registration & Dependency Injection](#service-registration--dependency-injection)
- [Type System & Deserialization](#type-system--deserialization)
- [Query Execution Pipeline](#query-execution-pipeline)
- [Key Architectural Patterns](#key-architectural-patterns)
- [Extension Points](#extension-points)
- [Testing Architecture](#testing-architecture)
- [Performance Optimizations](#performance-optimizations)

## Architecture Overview

The SDK follows a **layered architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────────┐
│  Public API (IDeliveryClient, Builders)     │
├─────────────────────────────────────────────┤
│  Query Builders (typed + dynamic wrappers)  │
├─────────────────────────────────────────────┤
│  HTTP Pipeline (Handlers, Refit)            │
├─────────────────────────────────────────────┤
│  Deserialization & Post-Processing          │
├─────────────────────────────────────────────┤
│  Caching Layer (Optional)                   │
├─────────────────────────────────────────────┤
│  Kontent.ai Delivery API                    │
└─────────────────────────────────────────────┘
```

Dynamic item/list builders (`DynamicItemQuery`, `DynamicItemsQuery`) are implemented as wrappers over `ItemQuery<IDynamicElements>` and `ItemsQuery<IDynamicElements>`, then adapt responses to non-generic runtime-typed outputs.

**Core Principles:**
- **DI-First**: All components designed for dependency injection
- **Type Safety**: Leverages OneOf, records, and generic types
- **Configuration**: Uses fluent builders (`DeliveryOptionsBuilder`) for type-safe configuration
- **Async**: Proper async/await throughout, no sync-over-async
- **Testability**: Interface-based design enables easy mocking

## Refit API Layer

### API Interface Definition

**Location**: `Kontent.Ai.Delivery/Api/IDeliveryApi.*.cs`

The SDK uses **Refit** to generate HTTP client implementations from interface definitions:

```csharp
public partial interface IDeliveryApi
{
    /// <summary>
    /// Gets a single content item by codename
    /// </summary>
    [Get("/items/{codename}")]
    internal Task<IApiResponse<DeliveryItemResponse<TModel>>> GetItemInternalAsync<TModel>(
        string codename,
        [Query] SingleItemParams? queryParameters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple content items with filtering
    /// </summary>
    [Get("/items")]
    internal Task<IApiResponse<DeliveryItemListingResponse<TModel>>> GetItemsInternalAsync<TModel>(
        [Query] ListItemsParams? queryParameters = null,
        [Query] Dictionary<string, string>? filters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null);
}
```

**Key Design Decisions:**

1. **Query Objects over Primitives**: Parameters are grouped into records (`ListItemsParams`) instead of individual parameters
   - Easier to extend without breaking changes
   - Better reusability across methods
   - Type-safe parameter validation

2. **Generic Methods**: `TModel` supports both strongly-typed and dynamic content
   - Strongly typed models are plain POCOs (no interface required)
   - Dynamic access uses `IDynamicElements` for dictionary-based element access

3. **Separate Filters Dictionary**: Filters passed as `Dictionary<string, string[]>` for maximum flexibility
   - Filters are dynamic (type-dependent operators)
   - Preserves repeated keys (duplicate filters) via multi-value arrays
   - Refit serializes as query parameters

### Refit Configuration

**Location**: `Kontent.Ai.Delivery/Configuration/RefitSettingsProvider.cs`

```csharp
public static RefitSettings CreateDefaultSettings()
{
    var jsonSerializerOptions = CreateDefaultJsonSerializerOptions();

    return new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions),
        CollectionFormat = CollectionFormat.Multi,  // ?tags=a&tags=b instead of ?tags=a,b
        UrlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter()
    };
}

public static JsonSerializerOptions CreateDefaultJsonSerializerOptions()
{
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Custom converters for complex types
    options.Converters.Add(new ContentItemConverterFactory());
    options.Converters.Add(new ContentElementConverter());

    return options;
}
```

**Why These Settings:**
- **System.Text.Json**: Modern, fast, built-in serializer
- **CamelCase**: Matches Kontent.ai API JSON structure
- **CollectionFormat.Multi**: Required for API array parameters
- **Custom converters**: Handle polymorphic deserialization

## Filtering (fluent DSL)

Filtering is expressed using a small DSL that **maps to** Delivery API filtering query parameters and operator suffixes.

The public surface is intentionally more ergonomic (verbose, discoverable method names), while the underlying serialization stays faithful to the wire format (e.g. `system.type[eq]=article`). The SDK also enforces endpoint capabilities at compile time (items vs types vs taxonomies expose different operator sets).

Internally, filtering is modularized into focused components:
- path normalization (`FilterPath`)
- operator/value serialization (`FilterSuffix`, `FilterValueSerializer`)
- endpoint-specific fluent builders (`ItemsFilterBuilder`, `TypesFilterBuilder`, `TaxonomiesFilterBuilder`)
- filter state and query conversion (`SerializedFilterCollection`)
- shared automatic system filter helpers (`SystemFilterHelpers`)

**Key ideas:**
- `.System("<property>")` targets `system.<property>`
- `.Element("<codename>")` targets `elements.<codename>` (items only)
- Operators match Delivery API semantics: `IsEqualTo`, `IsNotEqualTo`, `IsGreaterThan`, `IsGreaterThanOrEqualTo`, `IsLessThan`, `IsLessThanOrEqualTo`, `IsWithinRange`, `IsIn`, `IsNotIn`, `Contains`, `ContainsAny`, `ContainsAll`, `IsEmpty`, `IsNotEmpty`
- Filters are combined using logical **AND** (each operator adds another query parameter)

Example:

```csharp
var result = await client.GetItems()
    .Where(f => f
        .System("type").IsEqualTo("article")
        .Element("category").Contains("coffee")
        .System("last_modified").IsGreaterThan(DateTime.UtcNow.AddDays(-30)))
    .ExecuteAsync();
```

## HTTP Pipeline & Delegating Handlers

The HTTP request pipeline uses three delegating handlers in sequence:

```
Request Flow:
  ItemsQuery.ExecuteAsync()
      ↓
  [Resilience Handler]      ← Retry + timeout policies
      ↓
  [Tracking Handler]        ← Add SDK identification headers
      ↓
  [Authentication Handler]  ← Auth + base URL rewrite
      ↓
  Refit HTTP Client         ← Actual HTTP request
      ↓
  Kontent.ai API
```

### 1. Resilience Handler

**Location**: Configured in `ServiceCollectionExtensions.cs`

Uses Microsoft's Polly resilience library:

```csharp
private static void ConfigureDefaultResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
{
    // Retry with exponential backoff
    builder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,  // Randomize delays to prevent thundering herd
        ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Result?.StatusCode is
                HttpStatusCode.TooManyRequests or
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.InternalServerError or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout)
    });

    // Global timeout
    builder.AddTimeout(TimeSpan.FromSeconds(30));
}
```

**Why This Design:**
- **Transient failure handling**: Automatic retry for rate limits (429) and server errors (5xx)
- **Exponential backoff**: Prevents overwhelming the API
- **Jitter**: Randomization prevents synchronized retry storms
- **Configurable**: Users can provide custom resilience configuration

### 2. Tracking Handler

**Location**: `Kontent.Ai.Delivery/Handlers/TrackingHandler.cs`

Injects SDK identification headers:

```csharp
public sealed class TrackingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.AddSdkTrackingHeader();      // X-KC-SDKID: "nuget.org;..."
        request.Headers.AddSourceTrackingHeader();   // X-KC-SOURCE: "..."

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
```

**Purpose**: Kontent.ai analytics - track SDK usage and versions

### 3. Authentication & Endpoint Handler

**Location**: `Kontent.Ai.Delivery/Handlers/DeliveryAuthenticationHandler.cs`

**Most critical handler** - handles three responsibilities:

```csharp
public sealed class DeliveryAuthenticationHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<DeliveryOptions> _monitor;
    private readonly string? _name;  // For named clients

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get current options (monitors allow runtime config changes)
        var opts = _name is null
            ? _monitor.CurrentValue
            : _monitor.Get(_name);

        // ========== 1. AUTHENTICATION ==========
        var apiKey = opts.GetApiKey();  // Preview or secure access key
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        // ========== 2. BASE URL REWRITE ==========
        // Enables runtime switching between Production/Preview/Custom endpoints
        var baseUri = new Uri(opts.GetBaseUrl().TrimEnd('/'), UriKind.Absolute);
        request.RequestUri = new Uri(baseUri, request.RequestUri);

        // ========== 3. ENVIRONMENT ID INJECTION ==========
        var envId = opts.EnvironmentId?.Trim('/');
        if (!string.IsNullOrWhiteSpace(envId))
        {
            var uri = request.RequestUri;
            var path = uri.AbsolutePath;
            var envPrefix = "/" + envId;

            if (!path.StartsWith(envPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var newPath = envPrefix + path;
                request.RequestUri = new Uri(baseUri, newPath);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
```

**Key Features:**

1. **IOptionsMonitor Support**: Configuration changes apply to next request (no client recreation needed)
2. **Runtime Endpoint Switching**: Can switch Production ↔ Preview dynamically
3. **Environment ID Injection**: Automatically prefixes paths with `/{environmentId}/`
4. **Named Client Support**: Different configurations per named client

## Caching Architecture

### Cache Manager Interface

**Location**: `Kontent.Ai.Delivery.Abstractions/Caching/IDeliveryCacheManager.cs`

```csharp
public interface IDeliveryCacheManager
{
    CacheStorageMode StorageMode => CacheStorageMode.HydratedObject;

    Task<CacheResult<T>?> GetOrSetAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<CacheEntry<T>?>> factory,  // Invoked on cache miss
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task<bool> InvalidateAsync(string[] dependencyKeys, CancellationToken cancellationToken = default);
}
```

The factory-based `GetOrSetAsync` pattern enables FusionCache-native stampede protection (concurrent cache misses for the same key coalesce into a single factory invocation). The **factory** returns `CacheEntry<T>?` which bundles the value with its dependency tags; returning `null` signals "don't cache" (e.g., API failure). The **method itself** returns `CacheResult<T>?` — a record exposing the cached value (`Value`) and the dependency keys associated with it (`DependencyKeys`).

### Cache Storage Mode

**Location**: `Kontent.Ai.Delivery.Abstractions/Caching/CacheStorageMode.cs` and `IDeliveryCacheManager.cs`

Cache managers declare their storage strategy via the `StorageMode` property on `IDeliveryCacheManager`. Query builders use this to decide whether to cache raw JSON payloads (rehydrating on read) or fully hydrated objects.

Dynamic item/list query wrappers intentionally construct their inner typed queries with `cacheManager: null`, so these runtime-typed queries bypass SDK caching by design.

```csharp
public enum CacheStorageMode { HydratedObject = 0, RawJson = 1 }

public interface IDeliveryCacheManager
{
    CacheStorageMode StorageMode => CacheStorageMode.HydratedObject;
    // ... other members
}
```

### Diagnostics and Logging

The SDK is intentionally resilient: certain failures are treated as "best effort" and **do not break request execution**. To preserve debuggability, the SDK emits **Debug-level** logs for otherwise silent failures, including:

- Cache deserialization failures (treated as cache miss)
- API error body parsing failures (falls back to a generic error message + raw body snippet)

Enable Debug logging for `Kontent.Ai.Delivery` to surface these diagnostics when investigating production issues.

#### Enable SDK debug logs (Console)

```csharp
using Kontent.Ai.Delivery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();

    // Show Debug logs for the SDK (tune as needed)
    logging.SetMinimumLevel(LogLevel.Information);
    logging.AddFilter("Kontent.Ai.Delivery", LogLevel.Debug);
});

services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});

var provider = services.BuildServiceProvider();
```

### Cache Manager Implementations

**Package**: `Kontent.Ai.Delivery.Caching`

Both built-in cache managers are thin wrappers over a shared `FusionCacheManager` engine:

- **`MemoryCacheManager`** — creates a FusionCache instance with L1 only (hydrated objects, `CacheStorageMode.HydratedObject`)
- **`HybridCacheManager`** — creates a FusionCache instance with L1+L2 (raw JSON via `FusionCacheSystemTextJsonSerializer`, `CacheStorageMode.RawJson`)

**Invalidation Model:**

FusionCache tag-based invalidation replaces the former reverse-index approach. Each cache entry is tagged with its dependency keys (content items, assets, taxonomies, list scopes). Calling `InvalidateAsync` expires all entries sharing any of the specified tags — this is deterministic with no race conditions.

```
Cache Entry: "items:type=article:lang=en"
  Tags: ["item_homepage", "item_featured_article", "taxonomy_categories", "scope_items_list"]

When InvalidateAsync(["item_homepage"]) is called:
  1. FusionCache expires all entries tagged with "item_homepage"
  2. If a backplane is configured, invalidation propagates to other nodes
```

`GetItems<T>()` query results additionally store `DeliveryCacheDependencies.ItemsListScope` (`scope_items_list`) as a synthetic dependency. This is a deliberate tradeoff to handle list-membership changes caused by item events (for example, new item publish matching a cached filter) without requiring a full cache purge.

`GetTypes()` and `GetTaxonomies()` follow the same pattern using `DeliveryCacheDependencies.TypesListScope` and `DeliveryCacheDependencies.TaxonomiesListScope`.

These list scopes complement per-entity dependencies (`type_{codename}`, `taxonomy_{codename}`): per-entity keys keep invalidation targeted for known entities, while scope keys handle list-membership changes caused by newly introduced entities.

### Cache Key Generation

**Location**: `Kontent.Ai.Delivery/Caching/CacheKeyBuilder.cs` (stays in the core package — no FusionCache dependency)

Generates **deterministic, human-readable, order-independent** cache keys:

```csharp
// Examples:
// item:homepage:lang=en-US:depth=2:elements=description|title
// items:type=article:lang=en-US:skip=0:limit=10:filters=A7F3E2B9

public static string BuildItemsKey(
    ListItemsParams parameters,
    IReadOnlyDictionary<string, string> filters)
{
    var builder = new StringBuilder(256);
    builder.Append("items").Append(Separator);

    AppendLanguage(builder, parameters.Language);
    AppendDepth(builder, parameters.Depth);
    AppendPagination(builder, parameters.Skip, parameters.Limit);
    AppendOrderBy(builder, parameters.OrderBy);
    AppendFilters(builder, filters);  // Sorted then hashed

    return TrimTrailingSeparator(builder);
}

private static void AppendFilters(
    StringBuilder builder,
    IReadOnlyDictionary<string, string> filters)
{
    if (filters.Count == 0) return;

    builder.Append("filters=");

    // Sort for determinism: [depth=2, lang=en] and [lang=en, depth=2] → same hash
    var sortedFilters = filters
        .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

    var filterString = string.Join("&",
        sortedFilters.Select(kvp => $"{kvp.Key}={kvp.Value}"));

    // Hash for brevity (first 12 chars of base64-encoded SHA256)
    var hash = ComputeStableHash(filterString);
    builder.Append(hash).Append(Separator);
}
```

**Design Benefits:**
- **Human-readable**: Easy debugging ("items:type=article" vs "8F3A9B2C")
- **Order-independent**: Same parameters → same key regardless of order
- **Deterministic**: Always produces same key for same parameters
- **Collision-resistant**: 12-char base64 hash = 2^72 combinations

### Dependency Tracking

**Location**: `Kontent.Ai.Delivery/ContentItems/Processing/ContentDependencyExtractor.cs`

Automatically extracts dependencies from content for cache invalidation:

```csharp
public sealed class ContentDependencyExtractor : IContentDependencyExtractor
{
    public void ExtractFromRichTextElement(
        IRichTextElementValue element,
        DependencyTrackingContext? context)
    {
        if (context == null) return;

        // Track inline images (by asset ID)
        if (element.Images != null)
        {
            foreach (var imageId in element.Images.Keys)
                context.TrackAsset(imageId);
        }

        // Track content links (by item codename)
        if (element.Links != null)
        {
            foreach (var link in element.Links.Values)
                context.TrackItem(link.Codename);
        }

        // Track embedded content items
        if (element.ModularContent != null)
        {
            foreach (var codename in element.ModularContent)
                context.TrackItem(codename);
        }
    }
}
```

## Rich Text Resolution System

### Resolution Pipeline

```
Rich Text Element JSON
      ↓
HTML Parser (AngleSharp)
      ↓
DOM Tree
      ↓
RichTextParser.ParseNodeAsync() (recursive)
      ↓
Node Type Discrimination:
  - <object data-codename="..."> → EmbeddedContent
  - <figure><img data-asset-id="..."> → InlineImage
  - <a data-item-id="..."> → ContentItemLink
  - Generic element → HtmlNode (recurse children)
  - Text → TextNode (leaf)
      ↓
IRichTextBlock instances
      ↓
HtmlResolver.ResolveAsync() (recursive)
      ↓
Resolved HTML string
```

### Rich Text Parser

**Location**: `Kontent.Ai.Delivery/ContentItems/Processing/RichTextParser.cs`

Parses HTML into a typed block tree:

```csharp
internal class RichTextParser(
    IHtmlParser parser,
    IContentDependencyExtractor dependencyExtractor,
    ILogger? logger = null)
{
    internal async Task<IRichTextContent?> ConvertAsync<TElement>(
        TElement contentElement,
        Func<string, Task<object>> getLinkedItem,
        DependencyTrackingContext? dependencyContext)
        where TElement : IContentElementValue<string>
    {
        if (contentElement is not IRichTextElementValue element)
            return null;

        // Parse HTML to DOM (synchronous)
        var document = parser.ParseDocument(element.Value);

        // Extract dependencies for caching
        dependencyExtractor.ExtractFromRichTextElement(element, dependencyContext);

        // Recursively parse DOM nodes to typed blocks
        var blocks = new List<IRichTextBlock>();
        foreach (var childNode in document.Body.ChildNodes)
        {
            var block = await ParseNodeAsync(childNode, element, getLinkedItem);
            if (block != null)
                blocks.Add(block);
        }

        var content = new RichTextContent
        {
            Links = element.Links,
            Images = element.Images,
            ModularContentCodenames = element.ModularContent
        };
        content.AddRange(blocks);

        return content;
    }

    private async Task<IRichTextBlock?> ParseNodeAsync(
        INode node,
        IRichTextElementValue element,
        Func<string, Task<object>> getLinkedItem)
    {
        return node switch
        {
            // Embedded content: <object type="application/kenticocloud" data-codename="...">
            IElement { TagName: "OBJECT" } el
                => await ParseEmbeddedContentAsync(el, getLinkedItem),

            // Inline image: <figure><img data-asset-id="..."></figure>
            IElement { TagName: "FIGURE" } el when TryGetInlineImage(el, element, out var image)
                => image,

            // Content link: <a data-item-id="...">...</a>
            IElement { TagName: "A" } el when TryGetItemId(el, out var itemId)
                => await ParseContentItemLinkAsync(el, itemId, element, getLinkedItem),

            // Generic HTML element (recurse)
            IElement el
                => await ParseHtmlElementAsync(el, element, getLinkedItem),

            // Text node
            IText text when !string.IsNullOrWhiteSpace(text.TextContent)
                => new TextNode(text.TextContent),

            _ => null
        };
    }
}
```

### HTML Resolver

**Location**: `Kontent.Ai.Delivery/ContentItems/RichText/Resolution/HtmlResolver.cs`

Recursively resolves blocks to HTML using configured resolvers:

```csharp
public sealed class HtmlResolver : IHtmlResolver
{
    private readonly Dictionary<Type, Delegate> _resolvers;
    private readonly HtmlResolverOptions _options;

    public async ValueTask<string> ResolveAsync(IRichTextContent content)
    {
        var builder = new StringBuilder(4096);  // Pre-sized for performance

        foreach (var block in content)
        {
            var html = await ResolveBlockAsync(block);
            builder.Append(html);
        }

        return builder.ToString();
    }

    private async ValueTask<string> ResolveBlockAsync(IRichTextBlock block)
    {
        return block switch
        {
            ITextNode text
                => await ResolveWithResolver(text),

            IInlineImage image
                => await ResolveWithResolver(image),

            IContentItemLink link
                => await ResolveContentItemLinkAsync(link),

            IEmbeddedContent content
                => await ResolveEmbeddedContentAsync(content),

            IHtmlNode html
                => await ResolveHtmlNodeAsync(html),

            _ => string.Empty
        };
    }

    private async ValueTask<string> ResolveWithResolver<T>(T block)
        where T : IRichTextBlock
    {
        if (_resolvers.TryGetValue(typeof(T), out var resolver))
        {
            var typedResolver = (BlockResolver<T>)resolver;

            return await typedResolver(
                block,
                resolveChildren: () => ResolveChildrenAsync(block));
        }

        return string.Empty;
    }
}
```

## Builder Patterns

### DeliveryOptionsBuilder

**Location**: `Kontent.Ai.Delivery/Configuration/DeliveryOptionsBuilder.cs`

Fluent API for configuration:

```csharp
public class DeliveryOptionsBuilder : IDeliveryOptionsBuilder
{
    private DeliveryOptions _options = new();

    public IDeliveryOptionsBuilder WithEnvironmentId(string environmentId)
    {
        _options.EnvironmentId = environmentId;
        return this;
    }

    public IDeliveryOptionsBuilder UsePreviewApi(string previewApiKey)
    {
        _options.UsePreviewApi = true;
        _options.PreviewApiKey = previewApiKey;
        _options.UseSecureAccess = false;
        return this;
    }

    public IDeliveryOptionsBuilder UseProductionApi(string? secureAccessApiKey = null)
    {
        _options.UsePreviewApi = false;
        _options.UseSecureAccess = secureAccessApiKey != null;
        _options.SecureAccessApiKey = secureAccessApiKey;
        return this;
    }

    public DeliveryOptions Build() => _options;
}

// Usage:
var options = DeliveryOptionsBuilder.CreateInstance()
    .WithEnvironmentId("...")
    .UsePreviewApi("preview-key")
    .Build();
```

### HtmlResolverBuilder

**Location**: `Kontent.Ai.Delivery/ContentItems/RichText/Resolution/HtmlResolverBuilder.cs`

Fluent API for rich text resolution configuration:

```csharp
public sealed class HtmlResolverBuilder : IHtmlResolverBuilder
{
    private readonly Dictionary<Type, Delegate> _resolvers = new();
    private readonly Dictionary<string, BlockResolver<IContentItemLink>> _linkResolvers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<IEmbeddedContent, ValueTask<string>>> _contentResolvers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ConditionalHtmlNodeResolver> _conditionalNodeResolvers = new();

    public IHtmlResolverBuilder WithContentItemLinkResolver(
        BlockResolver<IContentItemLink> resolver)
    {
        _resolvers[typeof(IContentItemLink)] = resolver;
        return this;
    }

    public IHtmlResolverBuilder WithContentItemLinkResolver(
        string contentTypeCodename,
        BlockResolver<IContentItemLink> resolver)
    {
        _linkResolvers[contentTypeCodename] = resolver;
        return this;
    }

    public IHtmlResolverBuilder WithContentResolver(
        string contentTypeCodename,
        Func<IEmbeddedContent, string> resolver)
    {
        // Wrap sync resolver in ValueTask
        _contentResolvers[contentTypeCodename] = content =>
            ValueTask.FromResult(resolver(content));
        return this;
    }

    public IHtmlResolver Build()
    {
        // Merge with default resolvers
        var resolversWithDefaults = new Dictionary<Type, Delegate>(_resolvers);

        // Add default text node resolver if not provided
        resolversWithDefaults.TryAdd(typeof(ITextNode),
            new BlockResolver<ITextNode>((text, _) =>
                ValueTask.FromResult(HtmlEncoder.Default.Encode(text.Text))));

        // Build options
        var options = new HtmlResolverOptions
        {
            EmbeddedContentResolvers = _contentResolvers,
            ContentItemLinkResolvers = _linkResolvers,
            ConditionalHtmlNodeResolvers = _conditionalNodeResolvers.ToArray()
        };

        return new HtmlResolver(resolversWithDefaults, options);
    }
}
```

## Service Registration & Dependency Injection

**Location**: `Kontent.Ai.Delivery/Extensions/ServiceCollectionExtensions.cs`

### Named Client Registration

```csharp
public static IServiceCollection AddDeliveryClient(
    this IServiceCollection services,
    string name,
    Action<DeliveryOptions> configureOptions,
    Action<IHttpClientBuilder>? configureHttpClient = null,
    Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null)
{
    // 1. Validate uniqueness
    if (services.Any(d => d.ServiceType == typeof(IDeliveryClient) && Equals(d.ServiceKey, name)))
        throw new InvalidOperationException($"Client '{name}' already registered.");

    // 2. Register named options
    services.Configure<DeliveryOptions>(name, configureOptions);
    services.AddOptions<DeliveryOptions>(name)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // 3. Register core dependencies (singleton, only once)
    RegisterDependencies(services);

    // 4. Register named HTTP client with Refit
    RegisterNamedHttpClient(services, name, configureHttpClient, configureResilience);

    // 5. Register keyed IDeliveryClient (.NET 8+)
    services.AddKeyedSingleton<IDeliveryClient>(name, (sp, key) =>
    {
        var clientName = (string)key;

        var api = sp.GetRequiredKeyedService<IDeliveryApi>(clientName);
        var contentItemMapper = sp.GetRequiredService<ContentItemMapper>();
        var contentDeserializer = sp.GetRequiredService<IContentDeserializer>();
        var typeProvider = sp.GetRequiredService<ITypeProvider>();
        var cacheManager = sp.GetKeyedService<IDeliveryCacheManager>(clientName);
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();

        return new DeliveryClient(
            api,
            contentItemMapper,
            contentDeserializer,
            typeProvider,
            cacheManager,
            logger: null,
            optionsMonitor,
            clientName);
    });

    return services;
}
```

Cache manager resolution is keyed-only. Unkeyed `IDeliveryCacheManager` registrations are ignored by `DeliveryClient` creation.
For built-in caching, use `AddDeliveryMemoryCache` / `AddDeliveryHybridCache` from the `Kontent.Ai.Delivery.Caching` package. For custom cache implementations, use `AddDeliveryCacheManager(clientName, factory)` to register per client.
Preview cache bypass is enforced by `DeliveryClient` itself (`UsePreviewApi = true` => no cache read/write for that client), not by a cache-manager decorator.

### Composing Options with `IServiceProvider`

All configure-callback overloads — `AddDeliveryClient`, `AddDeliveryMemoryCache`, `AddDeliveryHybridCache`, and the fluent `WithMemoryCache` / `WithHybridCache` — have a paired overload whose callback takes `IServiceProvider` alongside the options instance. Use this shape when an option value must be read from another service registered in the container, e.g. sibling options bound via `IOptions<T>`.

```csharp
services.Configure<SiteOptions>(configuration.GetSection("Site"));

services.AddDeliveryClient("production", (sp, opts) =>
{
    var site = sp.GetRequiredService<IOptions<SiteOptions>>().Value;
    opts.EnvironmentId = site.EnvironmentId;
});

services.AddDeliveryMemoryCache("production", (sp, opts) =>
{
    var site = sp.GetRequiredService<IOptions<SiteOptions>>().Value;
    opts.DefaultExpiration = TimeSpan.FromSeconds(site.CacheExpirationSeconds);
    opts.IsFailSafeEnabled = true;
});
```

Timing: for `AddDeliveryClient`, the callback runs when `IOptions<DeliveryOptions>` is first resolved (via `OptionsBuilder.Configure<IServiceProvider>`). For the cache overloads, the callback runs the first time the keyed singleton `IDeliveryCacheManager` is resolved from the root provider. Resolve only singleton-safe dependencies from cache callbacks, such as `IOptions<T>` or `IOptionsMonitor<T>`; do not depend on scoped/request services such as `IOptionsSnapshot<T>`. Validation of `DeliveryCacheOptions` is deferred to resolution time for the `(sp, opts)` cache overloads, whereas the plain `Action<DeliveryCacheOptions>` cache overloads validate eagerly at registration — use the plain overload if you need registration-time failure.

### Core Dependencies Registration

```csharp
private static void RegisterDependencies(IServiceCollection services)
{
    // JSON serialization options
    services.TryAddSingleton(RefitSettingsProvider.CreateDefaultJsonSerializerOptions());

    // Type system
    services.TryAddSingleton<ITypeProvider, TypeProvider>();
    services.TryAddSingleton<IItemTypingStrategy, DefaultItemTypingStrategy>();
    services.TryAddSingleton<IContentDeserializer, ContentDeserializer>();

    // Content mapping and HTML parsing
    services.TryAddSingleton<ElementValueMapper>();
    services.TryAddSingleton<LinkedItemResolver>();
    services.TryAddSingleton<ContentItemMapper>();
    services.TryAddSingleton<IHtmlParser, HtmlParser>();

    // Dependency extraction for cache invalidation and result dependency metadata
    services.TryAddSingleton<IContentDependencyExtractor, ContentDependencyExtractor>();
}
```

`ContentItemMapper` is the orchestration entry point. `ElementValueMapper` contains element-type mapping logic (rich text/assets/taxonomy/date-time/linked items/simple values), while `LinkedItemResolver` owns modular-content graph resolution, memoization, and circular reference behavior.

Rich text envelope JSON parsing is shared in `RichTextElementEnvelopeReader`, used by both mapper-side rich text hydration and dynamic `ParseRichTextAsync` parsing.

### HTTP Client Registration

```csharp
private static void RegisterNamedHttpClient(
    IServiceCollection services,
    string name,
    Action<IHttpClientBuilder>? configureHttpClient,
    Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience)
{
    var httpClientBuilder = services
        .AddRefitClient<IDeliveryApi>(RefitSettingsProvider.CreateDefaultSettings())
        .ConfigureHttpClient(client =>
        {
            // Base URL will be rewritten by DeliveryAuthenticationHandler
            client.BaseAddress = new Uri("https://deliver.kontent.ai");
        });

    // Add handlers in order (outermost to innermost)
    if (configureResilience != null)
    {
        httpClientBuilder.AddResilienceHandler("delivery-resilience", configureResilience);
    }
    else
    {
        httpClientBuilder.AddResilienceHandler("delivery-resilience", ConfigureDefaultResilience);
    }

    httpClientBuilder.AddHttpMessageHandler<TrackingHandler>();
    httpClientBuilder.AddHttpMessageHandler(sp =>
    {
        var monitor = sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        return new DeliveryAuthenticationHandler(monitor, name);
    });

    // Allow user customization
    configureHttpClient?.Invoke(httpClientBuilder);

    // Register keyed Refit interface
    services.AddKeyedSingleton(name, (sp, _) =>
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient(name);
        var refitSettings = RefitSettingsProvider.CreateDefaultSettings();
        return RestService.For<IDeliveryApi>(httpClient, refitSettings);
    });
}
```

## Type System & Deserialization

### Type Provider

**Purpose**: Map content type codenames to CLR types

With source generation enabled, type mapping is established at build time:

- `Kontent.Ai.Delivery.SourceGeneration` emits `ContentTypeCodenameAttribute` into the consuming compilation.
- The same build generates `Kontent.Ai.Delivery.Generated.GeneratedTypeProvider` from attributed model types.
- At runtime, the SDK's default `TypeProvider` auto-discovers the generated provider (entry assembly and referenced assemblies), then falls back to dynamic typing when no mapping is available.

```csharp
public interface ITypeProvider
{
    Type? GetType(string contentType);
    string? GetCodename(Type contentType);
}

// Default runtime implementation (discovers generated provider if present)
internal sealed class TypeProvider : ITypeProvider
{
    private static readonly ITypeProvider? _generated = DiscoverGeneratedProvider();

    public Type? GetType(string contentType) => _generated?.GetType(contentType);
    public string? GetCodename(Type contentType) => _generated?.GetCodename(contentType);

    private static ITypeProvider? DiscoverGeneratedProvider() => null; // Simplified for docs.
}

// Build-time generated implementation (by source generator)
public sealed class GeneratedTypeProvider : ITypeProvider
{
    private static readonly Dictionary<string, Type> _codenameToType = new(StringComparer.OrdinalIgnoreCase)
    {
        { "article", typeof(Article) },
        { "product", typeof(Product) },
        { "homepage", typeof(HomePage) }
    };
    private static readonly Dictionary<Type, string> _typeToCodename = new()
    {
        { typeof(Article), "article" },
        { typeof(Product), "product" },
        { typeof(HomePage), "homepage" }
    };

    public Type? GetType(string contentType)
        => _codenameToType.TryGetValue(contentType, out var type) ? type : null;

    public string? GetCodename(Type contentType)
        => _typeToCodename.TryGetValue(contentType, out var codename) ? codename : null;
}
```

### Content Deserializer

**Location**: `Kontent.Ai.Delivery/ContentItems/ContentDeserializer.cs`

Provides deserialization of JSON content items with cached compiled delegates for performance:

```csharp
internal sealed class ContentDeserializer : IContentDeserializer
{
    private readonly JsonSerializerOptions _options;

    // Cached compiled delegates per model type
    private static readonly ConcurrentDictionary<Type, Func<string, JsonSerializerOptions, object>> _stringDeserializers = new();
    private static readonly ConcurrentDictionary<Type, Func<JsonElement, JsonSerializerOptions, object>> _elementDeserializers = new();

    // String overload for backward compatibility
    public object DeserializeContentItem(string json, Type modelType)
    {
        var deserializer = _stringDeserializers.GetOrAdd(modelType, BuildStringDeserializer);
        return deserializer(json, _options);
    }

    // JsonElement overload to avoid GetRawText() allocations
    public object DeserializeContentItem(JsonElement jsonElement, Type modelType)
    {
        var deserializer = _elementDeserializers.GetOrAdd(modelType, BuildElementDeserializer);
        return deserializer(jsonElement, _options);
    }
}
```

### Custom JSON Converters

**ContentItemConverterFactory**: Dispatches to appropriate converter based on model type

```csharp
internal sealed class ContentItemConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ContentItem<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var modelType = typeToConvert.GetGenericArguments()[0];

        // Dispatch to appropriate converter based on model type
        return IsDynamicMode(modelType)
            ? (JsonConverter)Activator.CreateInstance(typeof(DynamicContentItemConverter<>).MakeGenericType(modelType))!
            : (JsonConverter)Activator.CreateInstance(typeof(StronglyTypedContentItemConverter<>).MakeGenericType(modelType))!;
    }

    private static bool IsDynamicMode(Type modelType)
        => modelType == typeof(IDynamicElements) || modelType == typeof(DynamicElements);
}
```

**StronglyTypedContentItemConverter**: Creates empty model instances for hydration

```csharp
internal sealed class StronglyTypedContentItemConverter<TModel> : JsonConverter<ContentItem<TModel>>
{
    public override ContentItem<TModel> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Parse system attributes
        var system = JsonSerializer.Deserialize<ContentItemSystemAttributes>(
            root.GetProperty("system"), options);

        // Clone full item JSON for post-processing
        var rawItemJson = root.Clone();

        // Create empty instance - ContentItemMapper will populate ALL properties
        var elements = Activator.CreateInstance<TModel>();

        return new ContentItem<TModel> { System = system, Elements = elements, RawItemJson = rawItemJson };
    }
}
```

**Key Architecture Decision**: The converters create minimal instances with `RawItemJson` preserved. Element mapping happens during post-processing via the mapping pipeline (`ContentItemMapper` orchestrating `ElementValueMapper` and `LinkedItemResolver`). This separation allows:
1. JSON deserializer stays simple and fast (sync)
2. Post-processing can be async (for linked items resolution)
3. Shared element envelope parsing (`RichTextElementEnvelopeReader`) across mapper and rich text converter paths
4. Easier testing and debugging

## Query Execution Pipeline

**Location**: `Kontent.Ai.Delivery/Api/QueryBuilders/ItemsQuery.cs`

The complete flow from query to cached result:

Dynamic item/list queries follow the same API execution and logging path by delegating to typed inner queries, then adapt the success payload to runtime-typed outputs. They intentionally skip caching.

```csharp
public async Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>> ExecuteAsync(
    CancellationToken cancellationToken = default)
{
    // ========== 1. CACHE-AWARE EXECUTION ==========
    // Uses factory-based GetOrSetAsync — FusionCache coalesces concurrent misses
    var cacheKey = BuildCacheKey(cacheManager.StorageMode);
    IDeliveryResult<DeliveryItemListingResponse<TModel>>? apiResult = null;

    var cached = await cacheManager.GetOrSetAsync(
        cacheKey,
        async ct =>
        {
            // ========== 2. API CALL (factory — only invoked on cache miss) ==========
            var result = await FetchFromApiAsync(waitForLoadingNewContent, ct);
            apiResult = result;
            if (!result.IsSuccess)
                return null;  // Don't cache failures

            // ========== 3. POST-PROCESSING + DEPENDENCY TRACKING ==========
            var (response, deps) = await ProcessItemsAsync(result.Value, ct);
            return new CacheEntry<DeliveryItemListingResponse<TModel>>(response, deps);
        },
        CacheExpiration,
        cancellationToken);

    // ========== 4. RESULT CLASSIFICATION ==========
    // Cache hit (factory never called) → CacheHit or FailSafeHit
    // Factory called, API succeeded → Success with fresh data
    // Factory called, API failed but stale data served → FailSafeHit
    return BuildResult(cached, apiResult);
}
```

## Key Architectural Patterns

### 1. Configuration via Builders

Configuration uses fluent builders for type-safe setup:

```csharp
// DeliveryOptions is a mutable configuration class with validation
public sealed class DeliveryOptions : IValidatableObject
{
    public string EnvironmentId { get; set; }
    public bool UsePreviewApi { get; set; }
    public string? PreviewApiKey { get; set; }
    // ... other properties with data annotations for validation
}

// Use DeliveryOptionsBuilder for fluent configuration
var options = DeliveryOptionsBuilder.CreateInstance()
    .WithEnvironmentId("your-env-id")
    .UsePreviewApi("your-preview-key")
    .Build();
```

### 2. Result Type Pattern

No throwing exceptions on API errors:

```csharp
public interface IDeliveryResult<out T>
{
    bool IsSuccess { get; }
    T? Value { get; }
    DeliveryError? Error { get; }
    string RequestUrl { get; }
    HttpStatusCode StatusCode { get; }
}

// Usage:
var result = await client.GetItem("homepage").ExecuteAsync();

if (result.IsSuccess)
{
    var item = result.Value;
    // Use item
}
else
{
    var error = result.Error;
    // Handle error
}

// Or throw if desired:
var item = result.ThrowIfFailure().Value;
```

### 3. Post-Execution Processing

Complex elements hydrated **after** deserialization, not during:

**Why:**
1. JSON deserializer stays simple and fast
2. Post-processing can be async
3. Dependency tracking happens in one place
4. Easier to test and debug

### 4. Dependency Injection First

Everything designed for DI:
- Interfaces for all abstractions
- Constructor injection everywhere
- No static methods or singletons
- Easy to mock for testing

### 5. Options Monitor Pattern

Configuration changes don't require recreating clients:

```csharp
public class DeliveryAuthenticationHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<DeliveryOptions> _monitor;

    protected override Task<HttpResponseMessage> SendAsync(...)
    {
        // Gets CURRENT options on each request
        var options = _monitor.CurrentValue;
        // ...
    }
}
```

## Extension Points

### For SDK Users

1. **Custom Type Providers**: Map content types to models
2. **Custom Property Mappers**: Map JSON properties to CLR properties
3. **Custom HTML Resolvers**: Control rich text rendering
4. **Custom Element Converters**: Transform element values during deserialization
5. **Custom Resilience Policies**: Configure retry/timeout behavior
6. **Custom Cache Managers**: Implement custom caching strategies

### For SDK Contributors

1. **New Query Builders**: Add support for new endpoints
2. **New Element Types**: Extend element type system
3. **New Resolvers**: Add new rich text block types
4. **Performance Optimizations**: Improve caching, parsing, serialization

## Testing Architecture

### Test Patterns

```csharp
[Fact]
public async Task GetItem_WithValidCodename_ReturnsItem()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("https://deliver.kontent.ai/*/items/homepage")
        .Respond("application/json", _fixtureJson);

    var services = new ServiceCollection();
    services.AddDeliveryClient(options =>
    {
        options.EnvironmentId = "test-env";
    });
    services.AddSingleton(mockHttp.ToHttpClient());

    var client = services.BuildServiceProvider()
        .GetRequiredService<IDeliveryClient>();

    // Act
    var result = await client.GetItem("homepage").ExecuteAsync();

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal("homepage", result.Value.System.Codename);
}
```

### Testing Tools

- **RichardSzalay.MockHttp**: Mock HTTP responses
- **Fixture files**: Reusable JSON responses from Kontent.ai
- **xUnit**: Test framework
- **FluentAssertions** (optional): Readable assertions

## Performance Optimizations

### 1. Property Caching

Reflection results cached per type:

```csharp
private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _writablePropertiesCache = new();

var properties = _writablePropertiesCache.GetOrAdd(
    elementType,
    static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanWrite)
        .ToArray());
```

### 2. StringBuilder Usage

Avoid string allocations:

```csharp
public static string BuildItemsKey(...)
{
    var builder = new StringBuilder(256);  // Pre-sized
    builder.Append("items").Append(Separator);
    // ... append more
    return builder.ToString();
}
```

### 3. Parallel Element Processing

Different element types processed concurrently:

```csharp
var tasks = new List<Task>
{
    ProcessRichTextGroupAsync(richTextProps, ...),
    ProcessAssetGroupAsync(assetProps, ...),
    ProcessTaxonomyGroupAsync(taxonomyProps, ...)
};

await Task.WhenAll(tasks);
```

### 4. Fine-Grained Locking

One semaphore per dependency key:

```csharp
private readonly ConcurrentDictionary<string, SemaphoreSlim> _dependencyLocks = new();

var lockInstance = _dependencyLocks.GetOrAdd(dependency, _ => new SemaphoreSlim(1, 1));
await lockInstance.WaitAsync(cancellationToken);
try
{
    // Update dependency
}
finally
{
    lockInstance.Release();
}
```

### 5. Frozen Collections

Immutable dictionaries for resolvers:

```csharp
private readonly FrozenDictionary<Type, Delegate> _resolvers;

public HtmlResolver(Dictionary<Type, Delegate> resolvers, ...)
{
    _resolvers = resolvers.ToFrozenDictionary();  // O(1) lookup, no allocations
}
```

---

## Contributing Guidelines

When contributing to the SDK:

1. **Follow existing patterns**: Use builders, records, interfaces
2. **Write tests**: All new features need tests
3. **Async all the way**: No sync-over-async
4. **Document public APIs**: XML docs for all public members
5. **Benchmark performance-critical code**: Use BenchmarkDotNet
6. **Handle errors gracefully**: Return results, don't throw (unless explicitly asked)

---

**Related Documentation:**
- [Main README](../README.md)
- [Performance Optimization Guide](performance-optimization.md)
- [Advanced Filtering Guide](advanced-filtering.md)
