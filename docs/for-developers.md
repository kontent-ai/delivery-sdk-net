# For Developers: SDK Internal Architecture

This document provides a comprehensive overview of the Kontent.ai Delivery .NET SDK's internal architecture for maintainers and contributors. It covers the implementation patterns, architectural decisions, and key components that make up the SDK.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Refit API Layer](#refit-api-layer)
- [Filter System with OneOf](#filter-system-with-oneof)
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
│  Query Builders (ItemQuery, ItemsQuery)     │
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

3. **Separate Filters Dictionary**: Filters passed as `Dictionary<string, string>` for maximum flexibility
   - Filters are dynamic (type-dependent operators)
   - Enables complex filter composition
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

## Filter System with OneOf

### Discriminated Union Types

**Location**: `Kontent.Ai.Delivery.Abstractions/QueryBuilders/Filtering/IItemFilters.cs`

The SDK uses **OneOf** library for type-safe discriminated unions:

```csharp
// Type aliases using OneOf
using ScalarValue = OneOf.OneOf<string, double, DateTime, bool>;
using ComparableValue = OneOf.OneOf<double, DateTime, string>;
using RangeBounds = OneOf.OneOf<(double Lower, double Upper), (DateTime Lower, DateTime Upper)>;
```

**Why OneOf:**
- **Compile-time safety**: Can't pass wrong types to filter operators
- **Exhaustive matching**: Compiler ensures all cases handled
- **Zero allocation**: No boxing overhead for value types
- **Pattern matching**: Clean, functional-style type discrimination

### Filter Value Hierarchy

**Location**: `Kontent.Ai.Delivery/Api/QueryBuilders/Filtering/FilterValue.cs`

Each filter value type implements `IFilterValue` for URL serialization:

```csharp
public interface IFilterValue
{
    string Serialize();
}

public sealed record StringValue(string Value) : IFilterValue
{
    public string Serialize() => UrlEncoder.Default.Encode(Value);
}

public sealed record NumericValue(double Value) : IFilterValue
{
    public string Serialize() => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed record DateTimeValue(DateTime Value) : IFilterValue
{
    public string Serialize() => Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}

public sealed record NumericRangeValue(double Lower, double Upper) : IFilterValue
{
    public string Serialize() => $"{Lower.ToString(CultureInfo.InvariantCulture)},{Upper.ToString(CultureInfo.InvariantCulture)}";
}
```

### OneOf Mapping Pattern

**Location**: `Kontent.Ai.Delivery/Api/QueryBuilders/Filtering/FilterValueMapper.cs`

The mapper uses OneOf's `Match` method for type-safe discrimination:

```csharp
internal static class FilterValueMapper
{
    /// <summary>
    /// Maps scalar value to concrete FilterValue type using pattern matching
    /// </summary>
    public static IFilterValue From(ScalarValue value) => value.Match<IFilterValue>(
        StringValue.From,        // Case: string
        NumericValue.From,       // Case: double
        DateTimeValue.From,      // Case: DateTime
        BooleanValue.From        // Case: bool
    );

    /// <summary>
    /// Maps comparable value to concrete FilterValue type
    /// </summary>
    public static IFilterValue From(ComparableValue value) => value.Match<IFilterValue>(
        NumericValue.From,       // Case: double
        DateTimeValue.From,      // Case: DateTime
        StringValue.From         // Case: string
    );

    /// <summary>
    /// Maps range bounds to concrete FilterValue type
    /// </summary>
    public static IFilterValue From(RangeBounds range) => range.Match<IFilterValue>(
        NumericRangeValue.From,  // Case: (double, double)
        DateRangeValue.From      // Case: (DateTime, DateTime)
    );
}
```

**Benefits:**
- **Type erasure eliminated**: No runtime type checking
- **Compiler verification**: All union members must be handled
- **Performance**: Direct delegate invocation, no reflection

### Filter Building Flow

```
User Code:
  .Filter(f => f.Equals(ItemSystemPath.Title, "Hello"))
         ↓
IItemFilters.Equals(path, ScalarValue)
         ↓
ScalarValue.Match() discriminates to StringValue
         ↓
IFilter.ToQueryParameter() -> ("system.title", "Hello")
         ↓
Added to _serializedFilters dictionary
         ↓
Serialized to query string: ?system.title=Hello
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
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,  // Content items/assets/taxonomies this entry depends on
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task InvalidateAsync(CancellationToken cancellationToken, params string[] dependencyKeys);
}
```

### Memory Cache Implementation

**Location**: `Kontent.Ai.Delivery/Caching/MemoryCacheManager.cs`

Uses **dual-index architecture** for dependency-based invalidation:

```csharp
public sealed class MemoryCacheManager : IDeliveryCacheManager
{
    private readonly IMemoryCache _cache;

    // Reverse index: dependency → set of cache keys that depend on it
    private readonly ConcurrentDictionary<string, HashSet<string>> _reverseIndex = new();

    // Per-entry cancellation tokens for immediate eviction
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();

    // Prevents race conditions when updating same dependency
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _dependencyLocks = new();
}
```

**Invalidation Model:**

```
Cache Entry: "items:type=article:lang=en"
  Dependencies: ["item_homepage", "item_featured_article", "taxonomy_categories"]

Reverse Index:
  "item_homepage"          → {"items:type=article:lang=en", ...}
  "item_featured_article"  → {"items:type=article:lang=en", ...}
  "taxonomy_categories"    → {"items:type=article:lang=en", ...}

When InvalidateAsync(["item_homepage"]) is called:
  1. Lookup reverse index["item_homepage"] → get all affected cache keys
  2. Cancel their CancellationTokens
  3. IMemoryCache automatically evicts them (via PostEvictionCallback)
  4. Clean up reverse index entries
```

**Thread Safety Strategy:**
- `ConcurrentDictionary` for lock-free primary operations
- `SemaphoreSlim` per dependency key prevents race conditions during updates
- Post-eviction callbacks clean up reverse index synchronously

### Cache Key Generation

**Location**: `Kontent.Ai.Delivery/Caching/CacheKeyBuilder.cs`

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

    // Hash for brevity (first 8 chars of base64-encoded SHA256)
    var hash = ComputeStableHash(filterString);
    builder.Append(hash).Append(Separator);
}
```

**Design Benefits:**
- **Human-readable**: Easy debugging ("items:type=article" vs "8F3A9B2C")
- **Order-independent**: Same parameters → same key regardless of order
- **Deterministic**: Always produces same key for same parameters
- **Collision-resistant**: 8-char base64 hash = 2^48 combinations

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
internal class RichTextParser : IElementValueConverter<string, IRichTextContent>
{
    private readonly IHtmlParser _parser;  // AngleSharp
    private readonly IContentDependencyExtractor _dependencyExtractor;

    internal async Task<IRichTextContent?> ConvertAsync<TElement>(
        TElement contentElement,
        ResolvingContext context,
        DependencyTrackingContext? dependencyContext)
        where TElement : IContentElementValue<string>
    {
        if (contentElement is not IRichTextElementValue element)
            return null;

        // Parse HTML to DOM
        var document = await _parser.ParseDocumentAsync(element.Value);

        // Extract dependencies for caching
        _dependencyExtractor.ExtractFromRichTextElement(element, dependencyContext);

        // Recursively parse DOM nodes to typed blocks
        var blocks = new List<IRichTextBlock>();
        foreach (var childNode in document.Body.ChildNodes)
        {
            var block = await ParseNodeAsync(childNode, element, context);
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
        ResolvingContext context)
    {
        return node switch
        {
            // Embedded content: <object type="application/kenticocloud" data-codename="...">
            IElement { TagName: "OBJECT" } el
                => await ParseEmbeddedContentAsync(el, context),

            // Inline image: <figure><img data-asset-id="..."></figure>
            IElement { TagName: "FIGURE" } el when TryGetInlineImage(el, element, out var image)
                => image,

            // Content link: <a data-item-id="...">...</a>
            IElement { TagName: "A" } el when TryGetItemId(el, out var itemId)
                => await ParseContentItemLinkAsync(el, itemId, element, context),

            // Generic HTML element (recurse)
            IElement el
                => await ParseHtmlElementAsync(el, element, context),

            // Text node
            IText text when !string.IsNullOrWhiteSpace(text.TextContent)
                => new TextNode(text.TextContent),

            _ => null
        };
    }
}
```

### Resolution Context

**Purpose**: Enables lazy-loading of linked items during resolution

```csharp
public class ResolvingContext
{
    public Func<string, Task<object>>? GetLinkedItem { get; init; }
    public string? Language { get; init; }
}

// Created during post-processing:
private ResolvingContext CreateResolvingContext(
    IReadOnlyDictionary<string, JsonElement>? modularContent)
{
    return new ResolvingContext
    {
        GetLinkedItem = async codename =>
        {
            if (modularContent == null ||
                !modularContent.TryGetValue(codename, out var linkedItemJson))
                return null;

            // Extract content type
            var contentType = linkedItemJson
                .GetProperty("system")
                .GetProperty("type")
                .GetString();

            // Resolve CLR type from content type codename
            var modelType = _typeProvider.TryGetModelType(contentType)
                ?? typeof(DynamicContentItem);

            // Deserialize
            var json = linkedItemJson.GetRawText();
            return _deserializer.DeserializeContentItem(json, modelType);
        },
        Language = /* ... */
    };
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
    var registry = GetOrCreateRegistry(services);
    if (!registry.TryRegister(name))
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
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();

        // Wrap monitor to return named options
        var namedMonitor = new NamedOptionsMonitor<DeliveryOptions>(
            optionsMonitor,
            clientName);

        var api = sp.GetRequiredKeyedService<IDeliveryApi>(clientName);
        var processor = sp.GetRequiredService<IElementsPostProcessor>();
        var cacheManager = sp.GetService<IDeliveryCacheManager>();

        return new DeliveryClient(api, namedMonitor, processor, cacheManager);
    });

    return services;
}
```

### Core Dependencies Registration

```csharp
private static void RegisterDependencies(IServiceCollection services)
{
    // JSON serialization options
    services.TryAddSingleton(RefitSettingsProvider.CreateDefaultJsonSerializerOptions());

    // HTTP handlers
    services.TryAddTransient<TrackingHandler>();
    services.TryAddTransient<DeliveryAuthenticationHandler>();

    // Type system
    services.TryAddSingleton<IPropertyMapper, PropertyMapper>();
    services.TryAddSingleton<ITypeProvider, TypeProvider>();
    services.TryAddSingleton<IItemTypingStrategy, DefaultItemTypingStrategy>();
    services.TryAddSingleton<IContentDeserializer, ContentDeserializer>();

    // Post-processing
    services.TryAddSingleton<IElementsPostProcessor, ElementsPostProcessor>();
    services.TryAddSingleton<IHtmlParser, HtmlParser>();

    // Default to no-op extractor (overridden when caching enabled)
    services.TryAddSingleton<IContentDependencyExtractor>(
        NullContentDependencyExtractor.Instance);
}
```

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

```csharp
public interface ITypeProvider
{
    Type? TryGetModelType(string contentType);
    string? GetCodename(Type contentType);
}

// Default implementation (returns null → falls back to dynamic)
internal class TypeProvider : ITypeProvider
{
    public Type? TryGetModelType(string contentType) => null;
    public string? GetCodename(Type contentType) => null;
}

// Generated implementation (from Kontent.ai model generator)
public class GeneratedTypeProvider : ITypeProvider
{
    private readonly Dictionary<string, Type> _typeMap = new()
    {
        ["article"] = typeof(Article),
        ["product"] = typeof(Product),
        ["homepage"] = typeof(HomePage)
    };

    public Type? TryGetModelType(string contentType)
        => _typeMap.TryGetValue(contentType, out var type) ? type : null;

    public string? GetCodename(Type contentType)
        => _typeMap.FirstOrDefault(kvp => kvp.Value == contentType).Key;
}
```

### Content Deserializer

**Location**: `Kontent.Ai.Delivery/ContentItems/ContentDeserializer.cs`

```csharp
public class ContentDeserializer : IContentDeserializer
{
    private readonly JsonSerializerOptions _options;

    public IContentItem DeserializeContentItem(string itemJson, Type modelType)
    {
        return (IContentItem)JsonSerializer.Deserialize(itemJson, modelType, _options)!;
    }
}
```

### Custom JSON Converters

**ContentItemConverterFactory**: Handles `ContentItem<T>` deserialization

```csharp
public class ContentItemConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType &&
               typeToConvert.GetGenericTypeDefinition() == typeof(ContentItem<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ContentItemConverter<>).MakeGenericType(elementType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
```

## Query Execution Pipeline

**Location**: `Kontent.Ai.Delivery/Api/QueryBuilders/ItemsQuery.cs`

The complete flow from query to cached result:

```csharp
public async Task<IDeliveryResult<IReadOnlyList<IContentItem<TModel>>>> ExecuteAsync(
    CancellationToken cancellationToken = default)
{
    // ========== 1. CACHE CHECK ==========
    string? cacheKey = null;
    if (_cacheManager != null)
    {
        cacheKey = CacheKeyBuilder.BuildItemsKey(_params, _serializedFilters);
        var cached = await _cacheManager.GetAsync<IDeliveryResult<...>>(
            cacheKey, cancellationToken);

        if (cached != null)
            return cached;  // Cache hit - early return
    }

    // ========== 2. API CALL ==========
    var rawResponse = await _api.GetItemsInternalAsync<TModel>(
        _params,
        _serializedFilters,
        waitForLoadingNewContent: _waitForLoadingNewContent);

    var deliveryResult = await rawResponse.ToDeliveryResultAsync();

    if (!deliveryResult.IsSuccess)
        return DeliveryResult.Failure<...>(...);

    var response = deliveryResult.Value;
    var items = response.Items;

    // ========== 3. DEPENDENCY TRACKING (if caching enabled) ==========
    var dependencyContext = _cacheManager != null
        ? new DependencyTrackingContext()
        : null;

    // Track all items in response
    if (dependencyContext != null && items.Count > 0)
    {
        foreach (var item in items)
            dependencyContext.TrackItem(item.System.Codename);
    }

    // Track modular content
    if (dependencyContext != null && response.ModularContent != null)
    {
        foreach (var codename in response.ModularContent.Keys)
            dependencyContext.TrackItem(codename);
    }

    // ========== 4. POST-PROCESSING ==========
    // Hydrate rich text, assets, taxonomies
    foreach (var item in items)
    {
        await _elementsPostProcessor.ProcessAsync(
            item,
            response.ModularContent,
            dependencyContext,
            cancellationToken);
    }

    // ========== 5. BUILD RESULT ==========
    var result = DeliveryResult.Success<IReadOnlyList<IContentItem<TModel>>>(
        items,
        deliveryResult.RequestUrl,
        deliveryResult.StatusCode,
        deliveryResult.HasStaleContent,
        deliveryResult.ContinuationToken);

    // ========== 6. CACHE RESULT ==========
    if (_cacheManager != null && dependencyContext != null && cacheKey != null)
    {
        await _cacheManager.SetAsync(
            cacheKey,
            result,
            dependencyContext.Dependencies,  // All tracked dependencies
            expiration: null,
            cancellationToken);
    }

    return result;
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
