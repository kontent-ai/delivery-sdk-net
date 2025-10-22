# Kontent.ai Delivery SDK for .NET

![Last modified][last-commit]
[![Issues][issues-shield]][issues-url]
[![Contributors][contributors-shield]][contributors-url]
[![MIT License][license-shield]][license-url]
[![codecov][codecov-shield]][codecov-url]
[![NuGet][nuget-shield]][nuget-url]
[![Stack Overflow][stack-shield]](https://stackoverflow.com/tags/kontent-ai)

The official .NET SDK for the [Kontent.ai Delivery API](https://kontent.ai/learn/docs/apis/openapi/delivery-api/), enabling you to retrieve content from your Kontent.ai projects with a modern, type-safe, and highly extensible client library.

> [!IMPORTANT]
> The modernized delivery SDK is currently in beta. While the core functionality is stable and production-ready, some features are still being polished. All the documentation and implementation is subject to change prior to production release. Feedback is welcome in the corresponding [pull request](https://github.com/kontent-ai/delivery-sdk-net/pull/407).

## Table of Contents

- [Installation](#-installation)
- [Quick Start](#-quick-start)
- [Basic Usage](#-basic-usage)
  - [Setting Up the Delivery Client](#setting-up-the-delivery-client)
  - [Retrieving Content](#retrieving-content)
  - [Filtering and Querying](#filtering-and-querying)
  - [Working with Strongly-Typed Models](#working-with-strongly-typed-models)
  - [Rich Text Resolution](#rich-text-resolution)
  - [Multi-Language Support](#multi-language-support)
  - [Caching](#caching)
  - [Preview API](#preview-api)
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
    foreach (var item in result.Value)
    {
        Console.WriteLine($"- {item.System.Name}");
    }
}
```

#### Get Items with Pagination

For large datasets, use the items feed for efficient pagination:

```csharp
var query = client.GetItemsFeed()
    .OrderBy(ItemSystemPath.LastModified, true);

await foreach (var item in query.ExecuteAsync())
{
    Console.WriteLine($"Item: {item.System.Name}");
}
```

#### Get Content Types and Taxonomies

```csharp
// Get a content type
var typeResult = await client.GetType("article").ExecuteAsync();

// Get all taxonomies
var taxonomiesResult = await client.GetTaxonomies().ExecuteAsync();

// Get a specific taxonomy
var taxonomyResult = await client.GetTaxonomy("product_categories").ExecuteAsync();
```

### Filtering and Querying

The SDK provides a type-safe filtering API with support for various operators:

#### Basic Filtering

```csharp
var result = await client.GetItems()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    .Filter(f => f.Contains(Elements.GetPath("title"), "coffee"))
    .Limit(20)
    .ExecuteAsync();
```

#### Common Filter Operators

```csharp
// Equality
.Filter(f => f.Equals(ItemSystemPath.Type, "product"))
.Filter(f => f.NotEquals(ItemSystemPath.Collection, "archived"))

// Comparison (numbers, dates, strings)
.Filter(f => f.GreaterThan(Elements.GetPath("price"), 100.0))
.Filter(f => f.LessThanOrEqual(Elements.GetPath("rating"), 4.5))

// Range (inclusive)
.Filter(f => f.Range(Elements.GetPath("price"), (50.0, 500.0)))

// Array membership
.Filter(f => f.In(ItemSystemPath.Type, new[] { "article", "blog_post" }))

// Multi-value element matching
.Filter(f => f.Any(Elements.GetPath("tags"), "featured", "trending"))
.Filter(f => f.All(Elements.GetPath("categories"), "tech", "news"))

// Null/empty checks
.Filter(f => f.NotEmpty(Elements.GetPath("description")))
```

#### Ordering and Pagination

```csharp
var result = await client.GetItems()
    .OrderBy(ItemSystemPath.LastModified, descending: true)
    .Skip(0)
    .Limit(10)
    .ExecuteAsync();
```

#### Getting Total Count

```csharp
var result = await client.GetItems()
    .WithTotalCount()
    .Limit(10)
    .ExecuteAsync();

if (result.IsSuccess)
{
    Console.WriteLine($"Total items: {result.Value.TotalCount}");
    Console.WriteLine($"Returned: {result.Value.Items.Count}");
}
```

For more advanced filtering scenarios, see the [Advanced Filtering Guide](docs/advanced-filtering.md).

### Working with Strongly-Typed Models

The SDK supports strongly-typed models for compile-time safety and IntelliSense support. Using the SDK with strongly typed models is recommended.

#### Generate Models

> [!WARNING]
> Model generator has not been updated yet if you see this. See for example [Article.cs](./Kontent.Ai.Delivery.Tests/Models/ContentTypes/Article.cs) and its siblings for examples of the new model structure.

Use the [Kontent.ai Model Generator](https://github.com/kontent-ai/model-generator-net) to generate C# classes from your content types:

```bash
dotnet tool install -g Kontent.Ai.ModelGenerator
KontentModelGenerator --environmentid <your-environment-id> --outputdir Models
```

#### Use Strongly-Typed Models

```csharp
public record Article
{
    public string Title { get; set; }
    public string Summary { get; set; }
    public RichTextContent BodyCopy { get; set; }
    public DateTime PublishDate { get; set; }
    public IEnumerable<Author> Authors { get; set; }
}

// Query with strong typing
var result = await client.GetItems<Article>()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    .WithLanguage("en-US")
    .ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var article in result.Value)
    {
        Console.WriteLine($"{article.Title} - {article.PublishDate}");
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
    .WithContentItemLinkResolver("article", (link, _) =>
    {
        var url = $"/articles/{link.Metadata?.UrlSlug}";
        return ValueTask.FromResult($"<a href=\"{url}\">{link.Text}</a>");
    })
    .WithContentItemLinkResolver("product", (link, _) =>
    {
        var url = $"/shop/{link.Metadata?.UrlSlug}";
        return ValueTask.FromResult($"<a href=\"{url}\">{link.Text}</a>");
    })
    .Build();

var html = await article.BodyCopy.ToHtmlAsync(resolver);
```

#### Embedded Content Resolution

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver("tweet", content =>
    {
        var tweet = content.Elements as Tweet; // cast to your strongly typed model
        return $"<blockquote class=\"twitter-tweet\">{tweet.Text}<cite>{tweet.Author}</cite></blockquote>";
    })
    .WithContentResolver("video", async content =>
    {
        var videoId = content.Elements["video_id"]?.ToString(); // dynamic access without strongly typed model
        return $"<div class=\"video-wrapper\"><iframe src=\"https://youtube.com/embed/{videoId}\"></iframe></div>";
    })
    .Build();

var html = await article.BodyCopy.ToHtmlAsync(resolver);
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
    .Filter(f => f.Equals(ItemSystemPath.Codename, "article"))
    .WithLanguage("de-DE")
    .ExecuteAsync();
```

#### Language Fallbacks

Language fallbacks are configured in your Kontent.ai project. The SDK respects these settings automatically. If content is not available in the requested language, the SDK returns content according to your fallback configuration.

In order to ignore language callbacks, you can combine `.WithLanguage` and filtering on `system.language`, setting both to the desired language codename. See [Ignoring language fallbacks](https://kontent.ai/learn/develop/hello-world/get-localized-content/typescript#a-ignoring-language-fallbacks) in Kontent.ai documentation for more details.

#### Get Available Languages

```csharp
var result = await client.GetLanguages().ExecuteAsync();

if (result.IsSuccess)
{
    foreach (var language in result.Value)
    {
        Console.WriteLine($"{language.System.Name} ({language.System.Codename})");
    }
}
```

### Caching

The SDK supports both in-memory and distributed caching for improved performance.

#### Memory Cache

```csharp
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
})
.WithMemoryCache(defaultExpiration: TimeSpan.FromHours(1));
```

#### Distributed Cache (Redis, SQL Server, etc.)

```csharp
// First, register your distributed cache implementation
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Then add delivery client with distributed caching
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
})
.WithDistributedCache(defaultExpiration: TimeSpan.FromHours(2));
```

Caching is transparent - once configured, all queries are automatically cached. Cache keys are built from query parameters, ensuring proper cache hits.

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
    options.WaitForLoadingNewContent = false;

    // Default image rendition preset
    options.DefaultRenditionPreset = "default";

    // Custom endpoints (for proxy scenarios)
    options.ProductionEndpoint = "https://deliver.kontent.ai";
    options.PreviewEndpoint = "https://preview-deliver.kontent.ai";
});
```

You can also configure HTTP client behavior:

```csharp
services.AddDeliveryClient(
    options => options.EnvironmentId = "your-id",
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

### Strong Typing Synchronization

When using generated models:

- **Regenerate models** whenever content types change in Kontent.ai
- Handle optional properties with nullable types
- Consider versioning strategies if you have long-running deployments

## Advanced Documentation

For more advanced scenarios and in-depth guides, explore the following documentation:

- **[Advanced Filtering](docs/advanced-filtering.md)** - Complex queries, combining filters, performance optimization
- **[Rich Text Customization](docs/rich-text-customization.md)** - Custom resolvers, URL patterns, async resolution
- **[Caching Guide](docs/caching-guide.md)** - Cache strategies, invalidation, webhook integration
- **[Multi-Client Scenarios](docs/multi-client-scenarios.md)** - Named clients, multi-tenant architectures
- **[Custom Type Converters](docs/custom-type-converters.md)** - Extensibility through custom element converters
- **[Performance Optimization](docs/performance-optimization.md)** - Query optimization, monitoring, best practices

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
