# Kontent.ai Delivery SDK for .NET

![Last modified][last-commit]
[![Issues][issues-shield]][issues-url]
[![Contributors][contributors-shield]][contributors-url]
[![MIT License][license-shield]][license-url]
[![codecov][codecov-shield]][codecov-url]
[![NuGet][nuget-shield]][nuget-url]
[![Stack Overflow][stack-shield]](https://stackoverflow.com/tags/kontent-ai)

The official .NET SDK for the [Kontent.ai Delivery API](https://kontent.ai/learn/docs/apis/openapi/delivery-api/), enabling you to retrieve content from your Kontent.ai projects with a modern, type-safe, and highly extensible client library.

## Table of Contents

- [Installation](#-installation)
- [Quick Start](#-quick-start)
- [Basic Usage](#-basic-usage)
  - [Setting Up the Delivery Client](#setting-up-the-delivery-client)
  - [Retrieving Content](#retrieving-content)
  - [Content Types and Elements](#content-types-and-elements)
  - [Taxonomies](#taxonomies)
  - [Reference Lookups (Used In)](#reference-lookups-used-in)
  - [Filtering and Querying](#filtering-and-querying)
  - [Working with Strongly-Typed Models](#working-with-strongly-typed-models)
  - [Dynamic Content Access](#dynamic-content-access)
  - [Working with Linked Items](#working-with-linked-items)
  - [Rich Text Resolution](#rich-text-resolution)
  - [Multi-Language Support](#multi-language-support)
  - [Caching](#caching)
  - [Preview API](#preview-api)
  - [Asset Renditions](#asset-renditions)
  - [Image Transformation](#image-transformation)
- [Configuration Options](#-configuration-options)
- [Important Considerations](#-important-considerations)
- [Advanced Documentation](#-advanced-documentation)
- [Contributing](#-contributing)
- [License](#-license)

## Installation

Install the SDK via NuGet Package Manager:

```bash
dotnet add package Kontent.Ai.Delivery
```

Or via the Package Manager Console:

```powershell
Install-Package Kontent.Ai.Delivery
```

## Quick Start

Here's a minimal example to get you started:

```csharp
using Kontent.Ai.Delivery;
using Microsoft.Extensions.DependencyInjection;

// Set up dependency injection
var services = new ServiceCollection();

services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});

var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<IDeliveryClient>();

// Retrieve content
var result = await client.GetItem("homepage").ExecuteAsync();

if (result.IsSuccess)
{
    var item = result.Value;
    Console.WriteLine($"Title: {item.System.Name}");
}
```

## Basic Usage

### Setting Up the Delivery Client

The SDK is designed to work with .NET's dependency injection container. Register the `IDeliveryClient` in your `Program.cs` or `Startup.cs`:

#### Basic Registration

```csharp
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
```

#### Registration from Configuration

```csharp
// appsettings.json
{
  "DeliveryOptions": {
    "EnvironmentId": "your-environment-id",
    "UsePreviewApi": false
  }
}

// Program.cs
services.AddDeliveryClient(configuration, "DeliveryOptions");
```

#### Using the Builder Pattern

```csharp
services.AddDeliveryClient(builder =>
    builder.WithEnvironmentId("your-environment-id")
           .UseProductionApi()
           .Build());
```

#### Source-Generated Type Provider (Recommended)

When you use the `[ContentTypeCodename]` attribute on your model classes (see [Generate Models](#generate-models)), the SDK's source generator automatically creates a `GeneratedTypeProvider`. The SDK auto-discovers this provider at runtime - no manual registration needed.

```csharp
// Just register the delivery client - type provider is auto-discovered
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
```

The auto-discovery searches the entry assembly and its references for the generated provider.

#### Registering a Custom Type Provider

If you need to override the auto-discovered provider or use a custom implementation, register your type provider **before** calling `AddDeliveryClient()`:

```csharp
// Register a custom type provider first (takes precedence over auto-discovery)
services.AddSingleton<ITypeProvider, MyCustomTypeProvider>();

// Then register the delivery client
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
```

The SDK uses `TryAddSingleton` internally, so your registration takes precedence over the default type provider.

#### Without Dependency Injection

For console applications, scripts, or scenarios where DI is not available, use `DeliveryClientBuilder` directly:

```csharp
using Kontent.Ai.Delivery.Configuration;

// Simple usage with Production API
using var container = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithEnvironmentId("your-environment-id")
        .UseProductionApi()
        .Build())
    .Build();

var client = container.Client;

// With Preview API and caching (type provider is auto-discovered from source generation)
using var container = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithEnvironmentId("your-environment-id")
        .UsePreviewApi("your-preview-api-key")
        .Build())
    .WithMemoryCache(TimeSpan.FromMinutes(30))
    .Build();

// Or explicitly provide a type provider if needed
using var container = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithEnvironmentId("your-environment-id")
        .Build())
    .WithTypeProvider(new GeneratedTypeProvider())
    .Build();

var client = container.Client;
```

The builder supports:
- `.WithOptions(Func<IDeliveryOptionsBuilder, DeliveryOptions>)` - Configure delivery options (environment ID, API mode, etc.)
- `.WithTypeProvider(ITypeProvider)` - Custom type provider for strongly-typed models
- `.WithMemoryCache(TimeSpan?)` - Enable in-memory caching
- `.WithDistributedCache(IDistributedCache, TimeSpan?)` - Enable distributed caching

`IDeliveryOptionsBuilder.WithCustomEndpoint(...)` applies the same endpoint to both Production and Preview URLs. In most real deployments these endpoints differ, so if you need both modes with custom domains, register separate clients (for example named clients) and configure each with its corresponding endpoint.

### Retrieving Content

#### Get a Single Item

```csharp
// By codename
var result = await client.GetItem("coffee_beverages_explained")
    .ExecuteAsync();

if (result.IsSuccess)
{
    var article = result.Value;
    Console.WriteLine($"Title: {article.System.Name}");
}
```

#### Get Multiple Items

```csharp
var result = await client.GetItems()
    .Limit(10)
    .ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var item in result.Value.Items)
    {
        Console.WriteLine($"- {item.System.Name}");
    }
}
```

#### Get Items with Pagination

For large datasets, use the items feed for paginated enumeration with continuation tokens (e.g. for search index building, data synchronization, or bulk exports):

```csharp
// Option 1: Enumerate all items one-by-one using IAsyncEnumerable
await foreach (var item in client.GetItemsFeed().EnumerateItemsAsync())
{
    Console.WriteLine($"Item: {item.System.Name}");
}

// Option 2: Manual page-by-page control using FetchNextPageAsync
var firstPage = await client.GetItemsFeed().ExecuteAsync();
if (firstPage.IsSuccess)
{
    foreach (var item in firstPage.Value.Items)
    {
        Console.WriteLine($"Item: {item.System.Name}");
    }

    // Fetch next page if available
    while (firstPage.Value.HasNextPage)
    {
        var nextPage = await firstPage.Value.FetchNextPageAsync();
        if (nextPage?.IsSuccess == true)
        {
            foreach (var item in nextPage.Value.Items)
            {
                Console.WriteLine($"Item: {item.System.Name}");
            }
            firstPage = nextPage;
        }
        else break;
    }
}
```

For standard skip/limit pagination with `GetItems()`, use `FetchNextPageAsync()` to iterate through pages:

```csharp
var firstPage = await client.GetItems<Article>()
    .Limit(10)
    .WithTotalCount()
    .ExecuteAsync();

if (firstPage.IsSuccess)
{
    // Process first page
    foreach (var item in firstPage.Value.Items)
    {
        Console.WriteLine($"Item: {item.System.Name}");
    }

    // Fetch next page if available
    if (firstPage.Value.HasNextPage)
    {
        var nextPage = await firstPage.Value.FetchNextPageAsync();
        // Continue processing...
    }
}
```

### Content Types and Elements

Content types define the structure of your content. The SDK provides methods to retrieve content type definitions and their elements.

#### Get a Single Content Type

```csharp
var result = await client.GetType("article").ExecuteAsync();

if (result.IsSuccess)
{
    var contentType = result.Value;
    Console.WriteLine($"Type: {contentType.System.Name}");
    Console.WriteLine($"Codename: {contentType.System.Codename}");

    // Access element definitions
    foreach (var (codename, element) in contentType.Elements)
    {
        Console.WriteLine($"  - {element.Name} ({element.Type})");
    }
}
```

#### Get Multiple Content Types

```csharp
var result = await client.GetTypes()
    .Limit(10)
    .ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var contentType in result.Value.Types)
    {
        Console.WriteLine($"{contentType.System.Name}: {contentType.Elements.Count} elements");
    }

    // Pagination support
    if (result.Value.HasNextPage)
    {
        var nextPage = await result.Value.FetchNextPageAsync();
    }
}
```

#### Get a Specific Content Element

Retrieve a single element definition from a content type:

```csharp
var result = await client.GetContentElement("article", "body_copy").ExecuteAsync();

if (result.IsSuccess)
{
    var element = result.Value.Element;
    Console.WriteLine($"Element: {element.Name}");
    Console.WriteLine($"Type: {element.Type}");
}
```

### Taxonomies

Taxonomies provide hierarchical classification for your content.

#### Get a Single Taxonomy Group

```csharp
var result = await client.GetTaxonomy("product_categories").ExecuteAsync();

if (result.IsSuccess)
{
    var taxonomy = result.Value;
    Console.WriteLine($"Taxonomy: {taxonomy.System.Name}");

    // Access hierarchical terms
    foreach (var term in taxonomy.Terms)
    {
        PrintTerm(term, 0);
    }
}

void PrintTerm(ITaxonomyTermDetails term, int indent)
{
    var prefix = new string(' ', indent * 2);
    Console.WriteLine($"{prefix}- {term.System.Name} ({term.System.Codename})");

    // Recursively print child terms
    foreach (var childTerm in term.Terms)
    {
        PrintTerm(childTerm, indent + 1);
    }
}
```

#### Get Multiple Taxonomy Groups

```csharp
var result = await client.GetTaxonomies()
    .Limit(10)
    .ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var taxonomy in result.Value.Taxonomies)
    {
        Console.WriteLine($"{taxonomy.System.Name}: {taxonomy.Terms.Count} top-level terms");
    }
}
```

### Reference Lookups (Used In)

Find which content items reference a specific item or asset. This is useful for impact analysis before making changes.

`EnumerateItemsAsync()` follows continuation tokens automatically. If a subsequent page request fails, enumeration stops gracefully and returns items already received (no exception is thrown by default).

Use status-aware enumeration when you need explicit page failure handling:

```csharp
await foreach (var page in client.GetItemUsedIn("john_doe").EnumerateItemsWithStatusAsync())
{
    if (!page.IsSuccess)
    {
        Console.WriteLine($"Used-in lookup failed with {(int)page.StatusCode}: {page.Error?.Message}");
        break;
    }

    foreach (var usage in page.Value)
    {
        Console.WriteLine($"Referenced by: {usage.System.Name}");
    }
}
```

#### Find Items Using a Content Item

```csharp
// Find all items that reference the "john_doe" author
await foreach (var usage in client.GetItemUsedIn("john_doe").EnumerateItemsAsync())
{
    Console.WriteLine($"Referenced by: {usage.System.Name} ({usage.System.Type})");
}
```

#### Find Items Using an Asset

```csharp
// Find all items that use a specific asset
var assetCodename = "hero_image";
var usages = new List<IUsedInItem>();
await foreach (var usage in client.GetAssetUsedIn(assetCodename).EnumerateItemsAsync())
{
    usages.Add(usage);
    Console.WriteLine($"Asset used in: {usage.System.Name}");
}

if (usages.Count == 0)
{
    Console.WriteLine("Asset is not used anywhere - safe to delete");
}
```

### Filtering and Querying

The SDK provides a type-safe filtering API with support for various operators:

#### Basic Filtering

```csharp
var result = await client.GetItems()
    .Where(f => f
        .System("type").IsEqualTo("article")
        // [contains] is for arrays (taxonomy/linked items/multiple choice), not strings.
        // See Delivery API docs: https://kontent.ai/learn/docs/apis/delivery-api/filtering-parameters?sl=1
        .Element("category").Contains("coffee"))
    .Limit(20)
    .ExecuteAsync();
```

> [!TIP]
> When using strongly-typed queries with source generation (e.g., `GetItems<Article>()`), the `system.type` filter is added automatically based on the `[ContentTypeCodename]` attribute. You only need manual type filtering for dynamic queries.

#### Incremental query composition (deferred execution)

The query is not sent until you call `ExecuteAsync()`, so you can build it up conditionally:

```csharp
var query = client.GetItems()
    .Limit(20);

if (onlyArticles)
{
    query = query.Where(f => f.System("type").IsEqualTo("article"));
}

if (!includeArchived)
{
    query = query.Where(f => f.System("collection").IsNotEqualTo("archived"));
}

if (onlyCoffee)
{
    query = query.Where(f => f.Element("category").Contains("coffee"));
}

var result = await query.ExecuteAsync();
```

#### Common Filter Operators

```csharp
var query = client.GetItems()
    .Where(f => f
        // Equality
        .System("type").IsEqualTo("product")
        .System("collection").IsNotEqualTo("archived")
        // Comparison (numbers, dates, strings)
        .Element("price").IsGreaterThan(100.0)
        .Element("rating").IsLessThanOrEqualTo(4.5)
        // Range (inclusive)
        .Element("price").IsWithinRange(50.0, 500.0)
        // Array membership
        .System("type").IsIn("article", "blog_post")
        // Multi-value element matching
        .Element("tags").ContainsAny("featured", "trending")
        .Element("categories").ContainsAll("tech", "news")
        // Null/empty checks
        .Element("description").IsNotEmpty());
```

#### Ordering and Pagination

```csharp
var result = await client.GetItems()
    .OrderBy("system.last_modified", OrderingMode.Descending)
    .Skip(0)
    .Limit(10)
    .ExecuteAsync();
```

#### Getting Total Count

```csharp
var result = await client.GetItems()
    .WithTotalCount()
    .Limit(10)
    .ExecuteResponseAsync();

if (result.IsSuccess)
{
    // Total count is returned in response pagination metadata
    Console.WriteLine($"Total items: {result.Value.Pagination.TotalCount}");
    Console.WriteLine($"Returned: {result.Value.Items.Count}");
}
```

#### Element Projection

Reduce response size and improve performance by selecting only the elements you need:

```csharp
// Include only specific elements
var result = await client.GetItems<Article>()
    .WithElements("title", "summary", "url_slug")
    .Limit(20)
    .ExecuteAsync();

// Exclude specific elements (get all except these)
var result = await client.GetItems<Article>()
    .WithoutElements("body_copy", "metadata")
    .Limit(20)
    .ExecuteAsync();
```

> [!TIP]
> For listing pages that only show titles and summaries, use `.WithElements()` to reduce payload size by 50-80%.

For more advanced filtering scenarios, see the [Advanced Filtering Guide](docs/advanced-filtering.md).

### Working with Strongly-Typed Models

The SDK supports strongly-typed models for compile-time safety and IntelliSense support. Using the SDK with strongly typed models is recommended.

#### Generate Models

Use the [Kontent.ai Model Generator](https://github.com/kontent-ai/model-generator-net) to generate C# classes from your content types:

```bash
dotnet tool install -g Kontent.Ai.ModelGenerator
KontentModelGenerator --environmentid <your-environment-id> --outputdir Models
```

#### Source Generation for Type Resolution

The [Kontent.ai Model Generator](https://github.com/kontent-ai/model-generator-net) automatically includes the `[ContentTypeCodename]` attribute on generated model classes. When combined with the source generation package, this provides:

- **Compile-time validation** - Duplicate codenames and invalid configurations are caught during build
- **Auto-discovered type provider** - No manual DI registration needed
- **Automatic type filtering** - Generic queries like `GetItems<Article>()` automatically add `system.type=article` filter

Add the source generation package to enable these features:

```xml
<PackageReference Include="Kontent.Ai.Delivery.SourceGeneration" Version="19.0.0" />
```

Generated models include the attribute automatically:

```csharp
// Generated by Kontent.ai Model Generator
using Kontent.Ai.Delivery.Attributes;

[ContentTypeCodename("article")]
public record Article
{
    public string Title { get; init; }
    public string Summary { get; init; }
    public RichTextContent BodyCopy { get; init; }
}

[ContentTypeCodename("product")]
public record Product
{
    public string Name { get; init; }
    public decimal Price { get; init; }
}
```

The source generator produces a `GeneratedTypeProvider` at compile time with bi-directional lookup (codename ↔ Type). The SDK auto-discovers this provider at runtime.

**Compile-time diagnostics:**
- `KDSG001`: Duplicate codename (error)
- `KDSG002`: Invalid codename - null, empty, or whitespace (error)
- `KDSG003`: Unsupported target type - interfaces and abstract classes (error)

#### Use Strongly-Typed Models

```csharp
public record Article
{
    public string Title { get; set; }
    public string Summary { get; set; }
    public RichTextContent BodyCopy { get; set; }
    public DateTime PublishDate { get; set; }
    public IEnumerable<IEmbeddedContent> RelatedArticles { get; set; }
}

// Query with strong typing
var result = await client.GetItems<Article>()
    .WithLanguage("en-US")
    .ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var article in result.Value.Items)
    {
        Console.WriteLine($"{article.Elements.Title} - {article.Elements.PublishDate}");
    }
}
```

> [!NOTE]
> When using source generation with `[ContentTypeCodename("article")]`, the SDK automatically adds `system.type=article` filter to generic queries like `GetItems<Article>()`. You don't need to manually filter by type.

### Dynamic Content Access

When you don't have strongly-typed models or need to access content dynamically, use the typeless query methods (`GetItem()`, `GetItems()`, `GetItemsFeed()`). You may also use them for runtime type resolution, if your project uses generated models.

> [!NOTE]
> Dynamic item/list queries (`GetItem()` and `GetItems()`) are intentionally non-cacheable because their final result type is resolved at runtime. Even with SDK caching configured, these queries always fetch from the API and return `IsCacheHit == false`.

#### Retrieve Content Without Type Parameters

```csharp
// Get a single item dynamically
var result = await client.GetItem("homepage").ExecuteAsync();

if (result.IsSuccess)
{
    var item = result.Value;
    Console.WriteLine($"Name: {item.System.Name}");
    Console.WriteLine($"Type: {item.System.Type}");

    // Access elements via pattern matching to IDynamicElements
    if (item is IContentItem<IDynamicElements> dynamicItem)
    {
        if (dynamicItem.Elements.TryGetValue("title", out var titleElement))
        {
            Console.WriteLine($"Title: {titleElement}");
        }
    }
}

// Get multiple items dynamically
var itemsResult = await client.GetItems()
    .Where(f => f.System("type").IsEqualTo("article"))
    .Limit(10)
    .ExecuteAsync();

if (itemsResult.IsSuccess)
{
    foreach (var item in itemsResult.Value.Items)
    {
        Console.WriteLine($"- {item.System.Name}");
    }
}
```

#### Runtime Type Resolution with Type Provider

When using source generation with `[ContentTypeCodename]` attributes, the SDK auto-discovers the generated `ITypeProvider`. Typeless queries automatically resolve items to their strongly-typed models at runtime:

```csharp
// Type provider is auto-discovered from source generation - no manual registration needed
services.AddDeliveryClient(options => { ... });

// Typeless queries return runtime-typed results
var result = await client.GetItem("on_roasts").ExecuteAsync();

if (result.IsSuccess)
{
    // Pattern match to access strongly-typed content
    switch (result.Value)
    {
        case IContentItem<Article> article:
            Console.WriteLine($"Article: {article.Elements.Title}");
            Console.WriteLine($"Summary: {article.Elements.Summary}");
            break;
        case IContentItem<Product> product:
            Console.WriteLine($"Product: {product.Elements.Name}");
            Console.WriteLine($"Price: ${product.Elements.Price}");
            break;
        default:
            // Fallback to dynamic access
            Console.WriteLine($"Unknown type: {result.Value.System.Type}");
            break;
    }
}
```

This is particularly useful for:
- **Mixed content listings**: Displaying articles, products, and other types together
- **Search results**: Content types vary based on search query
- **Webhook handlers**: Processing content where type isn't known until runtime

Linked items and rich text embedded content within runtime-typed items are also automatically resolved to their strongly-typed models.

#### When to Use Dynamic Access

Dynamic access is intended for edge cases where strongly-typed models are impractical:

- **Prototyping**: Quick exploration before generating models
- **Migration/sync tools**: Bulk processing across all content types
- **Admin utilities**: Generic content inspection tools

> [!TIP]
> For production applications, always use [strongly-typed models](#working-with-strongly-typed-models). They provide compile-time safety, IntelliSense support, and better maintainability.

### Working with Linked Items

Linked items elements (modular content) are automatically hydrated to strongly-typed embedded content, providing compile-time type safety and runtime type resolution.

#### Defining Linked Items in Models

Linked items properties use `IEnumerable<IEmbeddedContent>` to support runtime typing where each item can be a different content type:

```csharp
public record Article
{
    [JsonPropertyName("title")]
    public string Title { get; init; }

    [JsonPropertyName("summary")]
    public string Summary { get; init; }

    [JsonPropertyName("related_articles")]
    public IEnumerable<IEmbeddedContent>? RelatedArticles { get; init; }

    [JsonPropertyName("recommended_products")]
    public IEnumerable<IEmbeddedContent>? RecommendedProducts { get; init; }
}
```

#### Accessing Linked Items with Type Safety

Use pattern matching to access strongly-typed content:

```csharp
var result = await client.GetItem<Article>("my-article").ExecuteAsync();

if (result.IsSuccess)
{
    var article = result.Value.Elements;

    // Pattern matching for type-safe access
    foreach (var linkedItem in article.RelatedArticles!)
    {
        switch (linkedItem)
        {
            case IEmbeddedContent<Article> relatedArticle:
                Console.WriteLine($"Related: {relatedArticle.Elements.Title}");
                Console.WriteLine($"  Summary: {relatedArticle.Elements.Summary}");
                break;

            case IEmbeddedContent<Product> product:
                Console.WriteLine($"Product: {product.Elements.Name}");
                Console.WriteLine($"  Price: ${product.Elements.Price}");
                break;
        }
    }
}
```

#### Filtering Linked Items by Type

Use LINQ to filter linked items by specific types:

```csharp
// Get only articles from mixed linked items
var articles = article.RelatedArticles!
    .OfType<IEmbeddedContent<Article>>()
    .ToList();

foreach (var relatedArticle in articles)
{
    // Direct access to strongly-typed elements
    Console.WriteLine($"Article: {relatedArticle.Elements.Title}");
}

// Get only products
var products = article.RecommendedProducts!
    .OfType<IEmbeddedContent<Product>>()
    .ToList();
```

#### Accessing Metadata

All linked items include metadata regardless of their type via the `System` property:

```csharp
foreach (var linkedItem in article.RelatedArticles!)
{
    // Access system metadata for all types
    Console.WriteLine($"Type: {linkedItem.System.Type}");
    Console.WriteLine($"Codename: {linkedItem.System.Codename}");
    Console.WriteLine($"Name: {linkedItem.System.Name}");
    Console.WriteLine($"ID: {linkedItem.System.Id}");

    // Then access type-specific elements
    if (linkedItem is IEmbeddedContent<Article> typedArticle)
    {
        Console.WriteLine($"Title: {typedArticle.Elements.Title}");
    }
}
```

#### Extracting Element Models

You can extract just the element models without the `IEmbeddedContent` wrapper:

```csharp
// Get just the element models using LINQ
var articleElements = article.RelatedArticles!
    .OfType<IEmbeddedContent<Article>>()
    .Select(a => a.Elements)
    .ToList();

foreach (var articleElement in articleElements)
{
    // Direct access to model without IEmbeddedContent wrapper
    Console.WriteLine(articleElement.Title);
}
```

#### Mixed Content Types

Linked items elements can contain multiple content types, and all are preserved:

```csharp
public record HomePage
{
    [JsonPropertyName("featured_content")]
    public IEnumerable<IEmbeddedContent> FeaturedContent { get; init; }
}

var home = await client.GetItem<HomePage>("homepage").ExecuteAsync();

// Featured content might contain articles, products, videos, etc.
foreach (var item in home.Value.Elements.FeaturedContent)
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
            // Handle unknown types gracefully
            Console.WriteLine($"Unknown type: {item.System.Type}");
            break;
    }
}
```

### Rich Text Resolution

Rich text elements may contain structured content that needs to be resolved prior to being rendered.

#### Basic HTML Rendering

```csharp
var result = await client.GetItem<Article>("my-article").ExecuteAsync();

if (result.IsSuccess)
{
    var article = result.Value;

    // Use default resolver
    var html = await article.BodyCopy.ToHtmlAsync();
}
```

#### Custom Link Resolution

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver("article", async (link, resolveChildren) =>
    {
        var url = $"/articles/{link.Metadata?.UrlSlug}";
        var innerHtml = await resolveChildren(link.Children);
        return ValueTask.FromResult($"<a href=\"{url}\">{innerHtml}</a>");
    })
    .WithContentItemLinkResolver("product", async (link, resolveChildren) =>
    {
        var url = $"/shop/{link.Metadata?.UrlSlug}";
        var innerHtml = await resolveChildren(link.Children);
        return ValueTask.FromResult($"<a href=\"{url}\">{link.Text}</a>");
    })
    .Build();

var html = await article.BodyCopy.ToHtmlAsync(resolver);
```

#### Embedded Content Resolution

**Type-Safe Resolvers with Strongly-Typed Models:**

```csharp
var resolver = new HtmlResolverBuilder()
    // Type-safe resolver with compile-time checking
    .WithContentResolver<Tweet>(tweet =>
        $"<blockquote class=\"twitter-tweet\">{tweet.Elements.TweetText}<cite>@{tweet.Elements.AuthorHandle}</cite></blockquote>")
    // Async type-safe resolver
    .WithContentResolver<Video>(async video =>
    {
        var metadata = await _videoService.GetMetadataAsync(video.Elements.VideoId);
        return $"<div class=\"video-wrapper\"><iframe src=\"https://youtube.com/embed/{video.Elements.VideoId}\" title=\"{metadata.Title}\"></iframe></div>";
    })
    .Build();

var html = await article.BodyCopy.ToHtmlAsync(resolver);
```

**Codename-Based Resolvers:**

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver("tweet", content =>
    {
        // Requires manual casting
        if (content is IEmbeddedContent<Tweet> tweet)
        {
            return $"<blockquote>{tweet.Elements.TweetText}</blockquote>";
        }
        return string.Empty;
    })
    .Build();
```

Enable strict behavior when missing resolvers should fail fast:

```csharp
var resolver = new HtmlResolverBuilder()
    .ThrowOnMissingResolver()
    .WithContentResolver<Tweet>(tweet =>
        $"<blockquote>{tweet.Elements.TweetText}</blockquote>")
    .Build();
```

**Batch Registration with Tuples:**

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
                : "")
    )
    .Build();
```

#### Registering Resolver with Dependency Injection

To avoid creating the resolver at every call site, register `IHtmlResolver` in your DI container:

```csharp
// Program.cs - Register the resolver once
services.AddSingleton<IHtmlResolver>(sp => new HtmlResolverBuilder()
    .WithContentItemLinkResolver("article", async (link, resolveChildren) =>
    {
        var innerHtml = await resolveChildren(link.Children);
        return $"<a href=\"/articles/{link.Metadata?.UrlSlug}\">{innerHtml}</a>";
    })
    .WithContentResolver<Tweet>(tweet =>
        $"<blockquote>{tweet.Elements.TweetText}</blockquote>")
    .WithContentResolver<Video>(video =>
        $"<iframe src=\"https://youtube.com/embed/{video.Elements.VideoId}\"></iframe>")
    .Build());
```

Then inject and use it in your services:

```csharp
public class ArticleService
{
    private readonly IDeliveryClient _client;
    private readonly IHtmlResolver _resolver;

    public ArticleService(IDeliveryClient client, IHtmlResolver resolver)
    {
        _client = client;
        _resolver = resolver;
    }

    public async Task<string?> GetArticleHtmlAsync(string codename)
    {
        var result = await _client.GetItem<Article>(codename).ExecuteAsync();

        if (!result.IsSuccess)
            return null;

        return await result.Value.Elements.BodyCopy.ToHtmlAsync(_resolver);
    }
}
```

**Pattern Matching for Multiple Types:**

```csharp
// Access strongly-typed embedded content via pattern matching
foreach (var block in article.BodyCopy)
{
    switch (block)
    {
        case IEmbeddedContent<Tweet> tweet:
            Console.WriteLine($"Tweet: {tweet.Elements.TweetText}");
            break;
        case IEmbeddedContent<Video> video:
            Console.WriteLine($"Video: {video.Elements.Title}");
            break;
        case IEmbeddedContent<Quote> quote:
            Console.WriteLine($"Quote: {quote.Elements.Text}");
            break;
    }
}

// Or use extension methods for filtering
var tweets = article.BodyCopy.GetEmbeddedContent<Tweet>();
var tweetElements = article.BodyCopy.GetEmbeddedElements<Tweet>();
```

For advanced rich text scenarios including custom HTML nodes and complex resolution strategies, see the [Rich Text Customization Guide](docs/rich-text-customization.md).

### Multi-Language Support

Retrieve content in specific language variants:

#### Basic Language Variant Retrieval

```csharp
// Get Spanish version
var result = await client.GetItem("homepage")
    .WithLanguage("es-ES")
    .ExecuteAsync();

// Get all articles in German (strongly typed)
var articlesResult = await client.GetItems<Article>()
    .Where(f => f.System("type").IsEqualTo("article"))
    .WithLanguage("de-DE")
    .ExecuteAsync();
```

#### Language Fallbacks

Language fallbacks are configured in your Kontent.ai project. The SDK respects these settings automatically. If content is not available in the requested language, the SDK returns content according to your fallback configuration.

By default, `.WithLanguage("<lang>")` requests a language variant while still allowing fallbacks configured in Kontent.ai (this is equivalent to using the Delivery API `language=<lang>` parameter without also filtering by `system.language`).

To **disable language fallbacks** for list/feed queries (return only items that are actually translated into the requested language), use:

```csharp
var result = await client.GetItems<Article>()
    .WithLanguage("es-ES", LanguageFallbackMode.Disabled)
    .ExecuteAsync();
```

When `LanguageFallbackMode.Disabled` is used, the SDK automatically adds the equivalent of `system.language[eq]=<lang>` to the request (so the query uses both `language=<lang>` and `system.language=<lang>` as described in Kontent.ai docs).

> `GetItem(...)` single-item queries (typed and dynamic) do not support disabling language fallbacks. They only use `language=<lang>` and follow fallback behavior configured in Kontent.ai.

For list/feed queries, you can still achieve the same behavior manually by combining `.WithLanguage` and filtering on `system.language`, setting both to the desired language codename. See [Ignoring language fallbacks](https://kontent.ai/learn/develop/hello-world/get-localized-content/typescript#a-ignoring-language-fallbacks) in Kontent.ai documentation for more details.

#### Get Available Languages

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

### Caching

The SDK supports both in-memory and distributed caching for improved performance.

#### Memory Cache

```csharp
// Single client scenario
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
services.AddDeliveryMemoryCache(defaultExpiration: TimeSpan.FromHours(1));

// Multi-client scenario - use named clients
services.AddDeliveryClient("production", options => { ... });
services.AddDeliveryMemoryCache("production", defaultExpiration: TimeSpan.FromHours(1));
```

#### Distributed Cache (Redis, SQL Server, etc.)

```csharp
// First, register your distributed cache implementation
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Single client scenario
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
services.AddDeliveryDistributedCache(defaultExpiration: TimeSpan.FromHours(2));
```

Caching is transparent for cacheable query builders - once configured, cached query types are cached automatically and cache keys are built from query parameters for proper cache hits.

`GetItem()` and `GetItems()` dynamic queries are intentionally excluded from SDK caching (runtime-typed results), so they always return `IsCacheHit == false`.

> [!IMPORTANT]
> When `WaitForLoadingNewContent` is effectively enabled for a request (globally or via query override), the SDK bypasses its local cache for that request path (no cache read and no cache write).

**Cache payloads:** The in-memory cache stores hydrated objects for maximum performance. Distributed caches store raw JSON payloads (rehydrated on read) to avoid serialization issues with circular references.

If you implement a custom cache manager that stores raw payloads (typical for distributed caches), override the `StorageMode` property to return `CacheStorageMode.RawJson` so the SDK uses the raw JSON caching path.

#### Detecting Cache Hits

The SDK provides the `IsCacheHit` property on all delivery results to indicate when a response was served from the SDK's local cache:

```csharp
var result = await client.GetItem<Article>("my-article").ExecuteAsync();

if (result.IsSuccess)
{
    if (result.IsCacheHit)
    {
        // Response served from SDK cache (Memory or Distributed)
        // Note: ResponseHeaders, RequestUrl, and other metadata are not available for cache hits
        Console.WriteLine("Served from SDK cache");
    }
    else
    {
        // Response from API - headers are available
        Console.WriteLine($"Request URL: {result.RequestUrl}");

        // Check for CDN cache hit (Fastly)
        if (result.ResponseHeaders?.TryGetValues("X-Cache", out var cacheValues) == true)
        {
            Console.WriteLine($"CDN Cache: {string.Join(", ", cacheValues)}");
        }
    }
}
```

> [!NOTE]
> `IsCacheHit` indicates SDK-level caching only. For CDN-level cache information (Fastly), inspect the `ResponseHeaders` property for headers like `X-Cache`.

#### Purging the SDK Memory Cache

If you're using the SDK's in-memory cache (`AddDeliveryMemoryCache`), you can invalidate **all** cached entries at once using the optional `IDeliveryCachePurger` capability:

```csharp
using Kontent.Ai.Delivery.Abstractions;

var cacheManager = serviceProvider.GetRequiredService<IDeliveryCacheManager>();
if (cacheManager is IDeliveryCachePurger purger)
{
    await purger.PurgeAsync();
}
```

> [!NOTE]
> Purge-all is not supported for generic distributed caches (`IDistributedCache`). Use provider-specific tools or key-prefix rotation.

For advanced caching strategies including cache invalidation, webhook integration, and multi-tenant scenarios, see the [Caching Guide](docs/caching-guide.md).

### Preview API

The Preview API allows you to retrieve unpublished content for preview purposes.

#### Enable Preview API

```csharp
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
    options.UsePreviewApi = true;
    options.PreviewApiKey = "your-preview-api-key";
});
```

#### Dynamic Switching (Production vs Preview)

You can configure named clients for different environments:

```csharp
services.AddDeliveryClient("production", options =>
{
    options.EnvironmentId = "your-environment-id";
    options.UsePreviewApi = false;
});

services.AddDeliveryClient("preview", options =>
{
    options.EnvironmentId = "your-environment-id";
    options.UsePreviewApi = true;
    options.PreviewApiKey = "your-preview-api-key";
});

// Inject factory and get appropriate client
var factory = serviceProvider.GetRequiredService<IDeliveryClientFactory>();
var client = isPreviewMode ? factory.Get("preview") : factory.Get("production");
```

For more on named clients and multi-environment scenarios, see the [Multi-Client Scenarios Guide](docs/multi-client-scenarios.md).

### Asset Renditions

Assets can have pre-configured renditions (image presets) defined in Kontent.ai. Access these directly without applying additional transformations.

#### Accessing Asset Renditions

```csharp
var result = await client.GetItem<Article>("my-article").ExecuteAsync();

if (result.IsSuccess)
{
    var article = result.Value.Elements;

    foreach (var asset in article.TeaserImage)
    {
        // Original asset URL
        Console.WriteLine($"Original: {asset.Url}");
        Console.WriteLine($"Size: {asset.Width}x{asset.Height}");

        // Access pre-configured renditions
        if (asset.Renditions.TryGetValue("thumbnail", out var thumbnail))
        {
            Console.WriteLine($"Thumbnail: {thumbnail.Url}");
            Console.WriteLine($"Thumbnail size: {thumbnail.Width}x{thumbnail.Height}");
        }

        if (asset.Renditions.TryGetValue("hero", out var hero))
        {
            Console.WriteLine($"Hero: {hero.Url}");
        }
    }
}
```

#### Default Rendition Preset

Configure a default rendition preset to use across all asset URLs:

```csharp
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
    options.DefaultRenditionPreset = "web";  // Apply "web" preset to all assets
});
```

When set, all asset URLs returned by the SDK will automatically include the specified rendition preset's transformations.

### Image Transformation

The SDK includes `ImageUrlBuilder` for dynamically transforming images served from Kontent.ai. This allows you to resize, crop, and optimize images on-the-fly without storing multiple versions.

#### Basic Usage

```csharp
using Kontent.Ai.Urls.ImageTransformation;

// Get an image URL from your content
var imageUrl = article.HeroImage.Url;

// Apply transformations
var transformedUrl = new ImageUrlBuilder(imageUrl)
    .WithWidth(800)
    .WithHeight(600)
    .WithFitMode(ImageFitMode.Crop)
    .Url;
```

#### Resizing

```csharp
// Resize to specific dimensions
var resized = new ImageUrlBuilder(imageUrl)
    .WithWidth(1200)
    .WithHeight(630)
    .Url;

// Resize with device pixel ratio for high-DPI displays
var retinaReady = new ImageUrlBuilder(imageUrl)
    .WithWidth(400)
    .WithDpr(2.0)  // Serves 800px image for 2x displays
    .Url;
```

#### Fit Modes

Control how the image fits within the target dimensions:

```csharp
// Clip: Fit within boundaries without cropping (default)
var clipped = new ImageUrlBuilder(imageUrl)
    .WithWidth(800)
    .WithHeight(600)
    .WithFitMode(ImageFitMode.Clip)
    .Url;

// Scale: Stretch to exact dimensions (may distort)
var scaled = new ImageUrlBuilder(imageUrl)
    .WithWidth(800)
    .WithHeight(600)
    .WithFitMode(ImageFitMode.Scale)
    .Url;

// Crop: Fill dimensions and crop excess
var cropped = new ImageUrlBuilder(imageUrl)
    .WithWidth(800)
    .WithHeight(600)
    .WithFitMode(ImageFitMode.Crop)
    .Url;
```

#### Cropping

```csharp
// Rectangle crop: extract a specific region (x, y, width, height)
var rectangleCrop = new ImageUrlBuilder(imageUrl)
    .WithRectangleCrop(100, 50, 400, 300)
    .Url;

// Focal point crop: crop centered on a point with zoom
var focalPointCrop = new ImageUrlBuilder(imageUrl)
    .WithWidth(800)
    .WithHeight(600)
    .WithFocalPointCrop(0.5, 0.3, 1.5)  // x, y (0-1 normalized), zoom
    .Url;
```

#### Format Conversion and Optimization

```csharp
// Convert to WebP for smaller file sizes
var webp = new ImageUrlBuilder(imageUrl)
    .WithFormat(ImageFormat.Webp)
    .WithQuality(80)
    .Url;

// Automatic WebP with fallback for unsupported browsers
var autoFormat = new ImageUrlBuilder(imageUrl)
    .WithAutomaticFormat(ImageFormat.Jpg)
    .Url;

// Control WebP compression mode
var lossless = new ImageUrlBuilder(imageUrl)
    .WithFormat(ImageFormat.Webp)
    .WithCompression(ImageCompression.Lossless)
    .Url;

// Progressive JPEG for better perceived loading
var progressive = new ImageUrlBuilder(imageUrl)
    .WithFormat(ImageFormat.Pjpg)
    .WithQuality(85)
    .Url;
```

#### Combining Transformations

All transformations can be chained together:

```csharp
var optimizedHero = new ImageUrlBuilder(imageUrl)
    .WithWidth(1920)
    .WithHeight(1080)
    .WithFitMode(ImageFitMode.Crop)
    .WithFocalPointCrop(0.5, 0.4, 1.0)
    .WithFormat(ImageFormat.Webp)
    .WithQuality(80)
    .Url;
```

#### Available Formats

| Format | Enum Value | Description |
|--------|------------|-------------|
| GIF | `ImageFormat.Gif` | Animated image support |
| PNG | `ImageFormat.Png` | Lossless with transparency |
| PNG8 | `ImageFormat.Png8` | 8-bit palette PNG |
| JPEG | `ImageFormat.Jpg` | Lossy compression |
| Progressive JPEG | `ImageFormat.Pjpg` | JPEG with progressive loading |
| WebP | `ImageFormat.Webp` | Modern format, best compression |

## Configuration Options

The `DeliveryOptions` class provides comprehensive configuration:

```csharp
services.AddDeliveryClient(options =>
{
    // Required: Your Kontent.ai environment ID
    options.EnvironmentId = "your-environment-id";

    // Preview API settings
    options.UsePreviewApi = false;
    options.PreviewApiKey = "your-preview-api-key";

    // Secured production API (if enabled in Kontent.ai)
    options.UseSecureAccess = false;
    options.SecureAccessApiKey = "your-secure-api-key";

    // Retry and resilience settings
    options.EnableResilience = true;

    // Content freshness
    // Note: requests with effective wait=true bypass SDK cache reads/writes.
    options.WaitForLoadingNewContent = false;

    // Default image rendition preset
    options.DefaultRenditionPreset = "default";

    // Custom endpoints (for proxy scenarios, set independently)
    options.ProductionEndpoint = "https://deliver.kontent.ai";
    options.PreviewEndpoint = "https://preview-deliver.kontent.ai";
});
```

If you configure options with `IDeliveryOptionsBuilder`, `WithCustomEndpoint(...)` is a convenience method that sets both endpoints to the same URL. For distinct preview/production custom domains, configure separate clients and set endpoints per client.

You can also configure HTTP client behavior:

```csharp
services.AddDeliveryClient(
	buildDeliveryOptions: builder => builder.WithEnvironmentId("your-environment-id").Build(),
    configureHttpClient: builder =>
    {
        builder.ConfigureHttpClient(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
    },
    configureResilience: builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(2)
        });
    });
```

## Important Considerations

### API Rate Limits

Kontent.ai enforces rate limits on API requests. The SDK includes built-in retry logic to handle transient failures, but you should:

- **Implement caching** as your first line of defense against rate limits
- Monitor your API usage in the Kontent.ai dashboard
- Use the items feed (`GetItemsFeed()`) for efficient bulk operations

### Depth Parameter

The Delivery API has default depth limitations for linked content:

```csharp
// Control how many levels of linked items to retrieve
var result = await client.GetItem("article")
    .Depth(2)  // Default is typically 1
    .ExecuteAsync();
```

**Important**: Higher depth values increase response size and processing time. Only increase when necessary.

### Preview API Security

**Never expose Preview API keys in client-side code.** Preview API keys should only be used in server-side applications. For web applications, implement a server-side preview endpoint that uses the Preview API on behalf of authenticated users.

### Caching Considerations

- **Cache keys** must be unique per query, language variant, and environment
- **Memory cache** can lead to memory pressure with large content - monitor your application's memory usage
- **Distributed cache** is recommended for production scenarios with multiple application instances
- Always implement **cache invalidation** strategies, ideally using webhooks
- **Cache hit semantics**: When `IsCacheHit` is `true`, properties like `ResponseHeaders`, `RequestUrl`, and `ContinuationToken` are not available (null). Use `IsCacheHit` to differentiate between API responses and cached results

### Strong Typing Synchronization

When using generated models:

- **Regenerate models** whenever content types change in Kontent.ai
- Handle optional properties with nullable types
- Consider versioning strategies if you have long-running deployments
- **Source generation benefits**: When using `[ContentTypeCodename]` attributes, the compiler catches duplicate codenames and invalid configurations at build time (diagnostics KDSG001-003)

### Error Handling

The SDK uses a result pattern instead of throwing exceptions for API errors. This makes error handling explicit and predictable.

#### Checking for Errors

```csharp
var result = await client.GetItem<Article>("my-article").ExecuteAsync();

if (result.IsSuccess)
{
    var article = result.Value;
    // Process article
}
else
{
    // Handle error
    var error = result.Error;
    Console.WriteLine($"Error: {error.Message}");
    Console.WriteLine($"Status: {result.StatusCode}");

    // Error details for debugging/logging
    if (error.RequestId != null)
        Console.WriteLine($"Request ID: {error.RequestId}");

    if (error.ErrorCode.HasValue)
        Console.WriteLine($"Error Code: {error.ErrorCode}");
}
```

#### IError Properties

| Property | Description |
|----------|-------------|
| `Message` | Human-readable error description |
| `RequestId` | Unique request ID for Kontent.ai support |
| `ErrorCode` | Kontent.ai-specific error code |
| `SpecificCode` | More specific error code |
| `Exception` | Underlying exception (for network errors, etc.) |

### Response Metadata

Every API response includes metadata for debugging, cache control, and monitoring.

#### Accessing Response Metadata

```csharp
var result = await client.GetItem<Article>("my-article").ExecuteAsync();

if (result.IsSuccess)
{
    // Request URL for debugging
    Console.WriteLine($"Request URL: {result.RequestUrl}");

    // HTTP status code
    Console.WriteLine($"Status: {result.StatusCode}");

    // Check if served from SDK cache
    if (result.IsCacheHit)
    {
        Console.WriteLine("Served from SDK cache");
    }
    else
    {
        // Response headers available for fresh responses
        if (result.ResponseHeaders != null)
        {
            // Check CDN cache status (Fastly)
            if (result.ResponseHeaders.TryGetValues("X-Cache", out var cacheValues))
            {
                Console.WriteLine($"CDN Cache: {string.Join(", ", cacheValues)}");
            }
        }
    }

    // Check if newer content might be available
    if (result.HasStaleContent)
    {
        Console.WriteLine("Content may be stale - newer version exists");
    }
}
```

#### IDeliveryResult Properties

| Property | Description |
|----------|-------------|
| `IsSuccess` | Whether the request succeeded |
| `Value` | The response content (when successful) |
| `Error` | Error details (when failed) |
| `StatusCode` | HTTP status code |
| `RequestUrl` | Full request URL for debugging |
| `ResponseHeaders` | HTTP response headers (null for cache hits) |
| `IsCacheHit` | Whether response was served from SDK cache |
| `HasStaleContent` | Whether newer content may be available |
| `ContinuationToken` | Pagination token (for feed responses) |

## Advanced Documentation

For more advanced scenarios and in-depth guides, explore the following documentation:

- **[Advanced Filtering](docs/advanced-filtering.md)** - Complex queries, combining filters, performance optimization
- **[Rich Text Customization](docs/rich-text-customization.md)** - Custom resolvers, URL patterns, async resolution
- **[Caching Guide](docs/caching-guide.md)** - Cache strategies, invalidation, webhook integration
- **[Multi-Client Scenarios](docs/multi-client-scenarios.md)** - Named clients, multi-tenant architectures
- **[Performance Optimization](docs/performance-optimization.md)** - Query optimization, monitoring, best practices
- **[Extensibility Guide](docs/extensibility-guide.md)** - Custom type providers, property mappers, SDK extension points

## Contributing

Contributions are welcome! Please see our [Contributing Guide](CONTRIBUTING.md) for details on how to:

- Report bugs and request features
- Submit pull requests
- Follow our coding standards
- Run tests locally

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Questions or feedback?** Visit our [GitHub Issues](https://github.com/kontent-ai/delivery-sdk-net/issues) or check the [Kontent.ai Developer Hub](https://kontent.ai/learn/docs).


[last-commit]: https://img.shields.io/github/last-commit/kontent-ai/delivery-sdk-net/vnext?style=for-the-badge
[contributors-shield]: https://img.shields.io/github/contributors/kontent-ai/delivery-sdk-net?style=for-the-badge
[contributors-url]: https://github.com/kontent-ai/delivery-sdk-net/graphs/contributors
[issues-shield]: https://img.shields.io/github/issues/kontent-ai/delivery-sdk-net.svg?style=for-the-badge
[issues-url]: https://github.com/kontent-ai/delivery-sdk-net/issues
[license-shield]: https://img.shields.io/github/license/kontent-ai/delivery-sdk-net?label=license&style=for-the-badge
[license-url]: https://github.com/kontent-ai/delivery-sdk-net/blob/main/LICENSE
[stack-shield]: https://img.shields.io/badge/Stack%20Overflow-ASK%20NOW-FE7A16.svg?logo=stackoverflow&logoColor=white&style=for-the-badge
[discord-shield]: https://img.shields.io/discord/821885171984891914?label=Discord&logo=Discord&logoColor=white&style=for-the-badge
[codecov-shield]: https://img.shields.io/codecov/c/github/kontent-ai/delivery-sdk-net/main.svg?style=for-the-badge
[codecov-url]: https://app.codecov.io/github/kontent-ai/delivery-sdk-net
[nuget-url]: https://www.nuget.org/packages/Kontent.Ai.Delivery
[nuget-shield]: https://img.shields.io/nuget/vpre/Kontent.Ai.Delivery.svg?style=for-the-badge
