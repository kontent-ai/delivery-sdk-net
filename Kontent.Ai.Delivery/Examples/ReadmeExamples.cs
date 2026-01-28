namespace Kontent.Ai.Delivery.Examples;

using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Resolution;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

public static class ReadmeExamples
{
    // Quick Start
    public static async Task QuickStartAsync()
    {
        var services = new ServiceCollection();

        services.AddDeliveryClient(options =>
        {
            options.EnvironmentId = "your-environment-id";
        });

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IDeliveryClient>();

        var result = await client.GetItem("homepage").ExecuteAsync();

        if (result.IsSuccess)
        {
            var item = result.Value;
            Console.WriteLine($"Title: {item.System.Name}");
        }
    }

    // Basic Registration
    public static void BasicRegistration(IServiceCollection services)
    {
        services.AddDeliveryClient(options =>
        {
            options.EnvironmentId = "your-environment-id";
        });
    }

    // Registration from Configuration (only demonstrates signature; not runnable here)
    public static void RegistrationFromConfiguration(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddDeliveryClient(configuration, "DeliveryOptions");
    }

    // Using the Builder Pattern
    public static void UsingBuilderPattern(IServiceCollection services)
    {
        services.AddDeliveryClient(builder =>
            builder.WithEnvironmentId("your-environment-id")
               .UseProductionApi()
               .Build());
    }

    // Get a Single Item (dynamic)
    public static async Task GetSingleItemAsync(IDeliveryClient client)
    {
        var result = await client.GetItem("coffee_beverages_explained").ExecuteAsync();
        if (result.IsSuccess)
        {
            var article = result.Value;
            Console.WriteLine($"Title: {article.System.Name}");
        }
    }

    // Get Multiple Items (dynamic)
    public static async Task GetMultipleItemsAsync(IDeliveryClient client)
    {
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
    }

    // Items feed pagination - Option 1: EnumerateItemsAsync (IAsyncEnumerable)
    public static async Task ItemsFeedEnumerateItemsAsync(IDeliveryClient client)
    {
        await foreach (var item in client.GetItemsFeed().EnumerateItemsAsync())
        {
            Console.WriteLine($"Item: {item.System.Name}");
        }
    }

    // Items feed pagination - Option 2: Manual pagination with FetchNextPageAsync
    public static async Task ItemsFeedFetchNextPageAsync(IDeliveryClient client)
    {
        var firstPage = await client.GetItemsFeed().ExecuteAsync();
        if (firstPage.IsSuccess)
        {
            foreach (var item in firstPage.Value.Items)
            {
                Console.WriteLine($"Item: {item.System.Name}");
            }

            // Fetch next page if available
            var currentPage = firstPage;
            while (currentPage.Value.HasNextPage)
            {
                var nextPage = await currentPage.Value.FetchNextPageAsync();
                if (nextPage?.IsSuccess == true)
                {
                    foreach (var item in nextPage.Value.Items)
                    {
                        Console.WriteLine($"Item: {item.System.Name}");
                    }
                    currentPage = nextPage;
                }
                else break;
            }
        }
    }

    // Items listing pagination - FetchNextPageAsync
    public static async Task ItemsListingFetchNextPageAsync(IDeliveryClient client)
    {
        var firstPage = await client.GetItems<Article>()
            .Limit(10)
            .WithTotalCount()
            .ExecuteAsync();

        if (firstPage.IsSuccess)
        {
            Console.WriteLine($"Total items: {firstPage.Value.Pagination.TotalCount}");

            foreach (var article in firstPage.Value.Items)
            {
                Console.WriteLine($"Article: {article.Elements.Title}");
            }

            // Fetch next page if available
            if (firstPage.Value.HasNextPage)
            {
                var nextPage = await firstPage.Value.FetchNextPageAsync();
                if (nextPage?.IsSuccess == true)
                {
                    foreach (var article in nextPage.Value.Items)
                    {
                        Console.WriteLine($"Article: {article.Elements.Title}");
                    }
                }
            }
        }
    }

    // Content types and taxonomies
    public static async Task TypesAndTaxonomiesAsync(IDeliveryClient client)
    {
        var typeResult = await client.GetType("article").ExecuteAsync();
        var taxonomiesResult = await client.GetTaxonomies().ExecuteAsync();
        var taxonomyResult = await client.GetTaxonomy("product_categories").ExecuteAsync();

        _ = typeResult;
        _ = taxonomiesResult;
        _ = taxonomyResult;
    }

    // Basic filtering
    public static async Task BasicFilteringAsync(IDeliveryClient client)
    {
        var result = await client.GetItems()
            .Where(f => f
                .System("type").IsEqualTo("article")
                .Element("category").Contains("coffee"))
            .Limit(20)
            .ExecuteAsync();

        _ = result;
    }

    // Conditional composition example (without LINQ-like .Where).
    public static async Task ConditionalFilteringAsync(IDeliveryClient client, bool onlyArticles)
    {
        var query = client.GetItems();
        if (onlyArticles)
        {
            query = query.Where(f => f.System("type").IsEqualTo("article"));
        }

        _ = await query.ExecuteAsync();
    }

    // Common filter operators
    public static async Task CommonFilterOperatorsAsync(IDeliveryClient client)
    {
        var result = await client.GetItems()
            .Where(f => f
                .System("type").IsEqualTo("product")
                .System("collection").IsNotEqualTo("archived")
                .Element("price").IsGreaterThan(100.0)
                .Element("rating").IsLessThanOrEqualTo(4.5)
                .Element("price").IsWithinRange(50.0, 500.0)
                .System("type").IsIn("article", "blog_post")
                .Element("tags").ContainsAny("featured", "trending")
                .Element("categories").ContainsAll("tech", "news")
                .Element("description").IsNotEmpty()
                .System("collection").IsIn("tech", "news"))
            .ExecuteAsync();

        _ = result;
    }

    // Ordering and pagination
    public static async Task OrderingAndPaginationAsync(IDeliveryClient client)
    {
        var result = await client.GetItems()
            .OrderBy("system.last_modified", OrderingMode.Descending)
            .Skip(0)
            .Limit(10)
            .ExecuteAsync();

        _ = result;
    }

    // Getting total count
    public static async Task GettingTotalCountAsync(IDeliveryClient client)
    {
        var result = await client.GetItems()
            .WithTotalCount()
            .Limit(10)
            .ExecuteAsync();

        if (result.IsSuccess)
        {
            // result.Value is a listing response; adapt to available API in your app
        }
    }

    // Strongly typed model example (definition only; usage depends on generator)
    // Models are now plain POCOs - no interface required!
    public record Article
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public RichTextContent BodyCopy { get; set; } = [];
        public DateTime PublishDate { get; set; }
        public IEnumerable<IEmbeddedContent>? RelatedArticles { get; set; }
    }

    public record Product
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public record Video
    {
        public string Title { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
    }

    public record HomePage
    {
        public IEnumerable<IEmbeddedContent> FeaturedContent { get; set; } = Array.Empty<IEmbeddedContent>();
    }

    public record Author(string Name);

    // Query with strong typing
    public static async Task StrongTypingQueryAsync(IDeliveryClient client)
    {
        var result = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .WithLanguage("en-US")
            .ExecuteAsync();

        if (result.IsSuccess)
        {
            foreach (var article in result.Value.Items)
            {
                Console.WriteLine($"{article.Elements.Title} - {article.Elements.PublishDate}");
            }
        }
    }

    // Linked Items - Accessing with Type Safety
    public static async Task LinkedItemsWithTypeSafetyAsync(IDeliveryClient client)
    {
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
    }

    // Linked Items - Filtering by Type
    public static async Task LinkedItemsFilteringByTypeAsync(IDeliveryClient client)
    {
        var result = await client.GetItem<Article>("my-article").ExecuteAsync();

        if (result.IsSuccess)
        {
            var article = result.Value.Elements;

            // Get only articles from mixed linked items
            var articles = article.RelatedArticles!
                .OfType<IEmbeddedContent<Article>>()
                .ToList();

            foreach (var relatedArticle in articles)
            {
                // Direct access to strongly-typed elements
                Console.WriteLine($"Article: {relatedArticle.Elements.Title}");
            }
        }
    }

    // Linked Items - Accessing Metadata
    public static async Task LinkedItemsAccessingMetadataAsync(IDeliveryClient client)
    {
        var result = await client.GetItem<Article>("my-article").ExecuteAsync();

        if (result.IsSuccess)
        {
            var article = result.Value.Elements;

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
        }
    }

    // Linked Items - Extracting Element Models
    public static async Task LinkedItemsExtractingElementModelsAsync(IDeliveryClient client)
    {
        var result = await client.GetItem<Article>("my-article").ExecuteAsync();

        if (result.IsSuccess)
        {
            var article = result.Value.Elements;

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
        }
    }

    // Linked Items - Mixed Content Types
    public static async Task LinkedItemsMixedContentTypesAsync(IDeliveryClient client)
    {
        var home = await client.GetItem<HomePage>("homepage").ExecuteAsync();

        if (home.IsSuccess)
        {
            // Featured content might contain articles, products, videos, etc.
            foreach (var item in home.Value.Elements.FeaturedContent)
            {
                switch (item)
                {
                    case IEmbeddedContent<Article> article:
                        Console.WriteLine($"Article: {article.Elements.Title}");
                        break;
                    case IEmbeddedContent<Product> product:
                        Console.WriteLine($"Product: {product.Elements.Name}");
                        break;
                    case IEmbeddedContent<Video> video:
                        Console.WriteLine($"Video: {video.Elements.Title}");
                        break;
                    default:
                        // Handle unknown types gracefully
                        Console.WriteLine($"Unknown type: {item.System.Type}");
                        break;
                }
            }
        }
    }

    // Basic HTML rendering for Rich Text
    public static async Task RichTextBasicRenderingAsync(IDeliveryClient client)
    {
        var result = await client.GetItem<Article>("my-article").ExecuteAsync();
        if (result.IsSuccess)
        {
            var article = result.Value;
            var html = await article.Elements.BodyCopy.ToHtmlAsync();
            _ = html;
        }
    }

    // Custom Link Resolution
    public static async Task RichTextCustomLinkResolutionAsync(IRichTextContent rich, IContentItemLink sampleLink)
    {
        var resolver = new HtmlResolverBuilder()
            .WithContentItemLinkResolver("article", async (link, resolveChildren) =>
            {
                var url = $"/articles/{link.Metadata?.UrlSlug}";
                var innerHtml = await resolveChildren(link.Children);
                return await ValueTask.FromResult($"<a href=\"{url}\">{innerHtml}</a>");
            })
            .WithContentItemLinkResolver("product", async (link, resolveChildren) =>
            {
                var url = $"/shop/{link.Metadata?.UrlSlug}";
                var innerHtml = await resolveChildren(link.Children);
                return await ValueTask.FromResult($"<a href=\"{url}\">{innerHtml}</a>");
            })
            .Build();

        var html = await rich.ToHtmlAsync(resolver);
        _ = html;
    }

    // Embedded Content Resolution
    public static async Task RichTextEmbeddedContentResolutionAsync(IRichTextContent rich)
    {
        var resolver = new HtmlResolverBuilder()
            .WithContentResolver("tweet", content =>
            {
                var tweet = content.Elements as Tweet; // sample cast
                return $"<blockquote class=\"twitter-tweet\">{tweet?.Text}<cite>{tweet?.Author}</cite></blockquote>";
            })
            .WithContentResolver("video", content =>
            {
                var videoId = content.Elements is IDictionary<string, object> dict && dict.TryGetValue("video_id", out var v)
                    ? v?.ToString()
                    : null;
                return new ValueTask<string>($"<div class=\"video-wrapper\"><iframe src=\"https://youtube.com/embed/{videoId}\"></iframe></div>");
            })
            .Build();

        var html = await rich.ToHtmlAsync(resolver);
        _ = html;
    }

    // Multi-language retrieval
    public static async Task MultiLanguageAsync(IDeliveryClient client)
    {
        var resultEs = await client.GetItem("homepage")
            .WithLanguage("es-ES")
            .ExecuteAsync();

        var articlesDe = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .WithLanguage("de-DE")
            .ExecuteAsync();

        _ = resultEs;
        _ = articlesDe;
    }

    // Get Languages
    public static async Task GetLanguagesAsync(IDeliveryClient client)
    {
        var result = await client.GetLanguages().ExecuteAsync();
        if (result.IsSuccess)
        {
            foreach (var language in result.Value.Languages)
            {
                Console.WriteLine($"{language.System.Name} ({language.System.Codename})");
            }
        }
    }

    // Memory cache configuration (single client)
    public static void MemoryCacheConfig(IServiceCollection services)
    {
        services.AddDeliveryClient(options =>
        {
            options.EnvironmentId = "your-environment-id";
        });
        services.AddDeliveryMemoryCache(defaultExpiration: TimeSpan.FromHours(1));
    }

    // Detecting cache hits and accessing response headers
    public static async Task DetectingCacheHitsAsync(IDeliveryClient client)
    {
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
    }

    // Distributed cache configuration (requires distributed cache registered; keep as demo)
    public static void DistributedCacheConfig(IServiceCollection services)
    {
        // First register distributed cache provider (e.g., Redis)
        // services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");

        services.AddDeliveryClient(options =>
        {
            options.EnvironmentId = "your-environment-id";
        });
        services.AddDeliveryDistributedCache(defaultExpiration: TimeSpan.FromHours(2));
    }

    // Preview API enablement
    public static void EnablePreviewApi(IServiceCollection services)
    {
        services.AddDeliveryClient(options =>
        {
            options.EnvironmentId = "your-environment-id";
            options.UsePreviewApi = true;
            options.PreviewApiKey = "your-preview-api-key";
        });
    }

    // Dynamic switching via named clients
    public static IDeliveryClient DynamicSwitching(IServiceProvider serviceProvider, bool isPreviewMode)
    {
        var factory = serviceProvider.GetRequiredService<IDeliveryClientFactory>();
        var client = isPreviewMode ? factory.Get("preview") : factory.Get("production");
        return client;
    }

    // Configuration options sample
    public static void ConfigureOptions(IServiceCollection services)
    {
        services.AddDeliveryClient(options =>
        {
            options.EnvironmentId = "your-environment-id";
            options.UsePreviewApi = false;
            options.PreviewApiKey = "your-preview-api-key";
            options.UseSecureAccess = false;
            options.SecureAccessApiKey = "your-secure-api-key";
            options.EnableResilience = true;
            options.WaitForLoadingNewContent = false;
            options.DefaultRenditionPreset = "default";
            options.ProductionEndpoint = "https://deliver.kontent.ai";
            options.PreviewEndpoint = "https://preview-deliver.kontent.ai";
        });
    }

    // HTTP client behavior configuration
    public static void ConfigureHttpAndResilience(IServiceCollection services)
    {
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
    }

    // Depth parameter example
    public static async Task DepthParameterAsync(IDeliveryClient client)
    {
        var result = await client.GetItem("article")
            .Depth(2)
            .ExecuteAsync();
        _ = result;
    }

    // Placeholder types used in examples
    public record Tweet(string Text, string Author);

    // ===== DeliveryClientBuilder Examples (Non-DI Scenarios) =====

    // Simple usage with Production API
    public static IDeliveryClientContainer BuilderSimpleUsage()
    {
        // Build() returns IDeliveryClientContainer - caller should dispose when done
        var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId("your-environment-id")
                .UseProductionApi()
                .Build())
            .Build();

        return container;
    }

    // Using environment ID as Guid
    public static IDeliveryClientContainer BuilderWithGuidEnvironmentId()
    {
        // Build() returns IDeliveryClientContainer - caller should dispose when done
        var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(Guid.Parse("550cec62-90a6-4ab3-b3e4-3d0bb4c04f5c"))
                .UseProductionApi()
                .Build())
            .Build();

        return container;
    }

    // With custom type provider
    public static IDeliveryClientContainer BuilderWithTypeProvider(ITypeProvider typeProvider)
    {
        // Caller owns the container and should dispose it when done
        var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId("your-environment-id")
                .UseProductionApi()
                .Build())
            .WithTypeProvider(typeProvider)
            .Build();

        return container;
    }

    // With memory cache
    public static IDeliveryClientContainer BuilderWithMemoryCache()
    {
        var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId("your-environment-id")
                .UseProductionApi()
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        return container;
    }

    // With distributed cache
    public static IDeliveryClientContainer BuilderWithDistributedCache(IDistributedCache distributedCache)
    {
        var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId("your-environment-id")
                .UseProductionApi()
                .Build())
            .WithDistributedCache(distributedCache, TimeSpan.FromHours(1))
            .Build();

        return container;
    }

    // With full options using WithOptions
    public static IDeliveryClientContainer BuilderWithFullOptions()
    {
        var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId("your-environment-id")
                .UsePreviewApi("your-preview-api-key")
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(15))
            .Build();

        return container;
    }

    // Combined: type provider + memory cache
    public static IDeliveryClientContainer BuilderFullExample(ITypeProvider typeProvider)
    {
        var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId("your-environment-id")
                .UseProductionApi()
                .WithDefaultRenditionPreset("mobile")
                .Build())
            .WithTypeProvider(typeProvider)
            .WithMemoryCache(TimeSpan.FromHours(1))
            .Build();

        return container;
    }
}
