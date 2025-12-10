namespace Kontent.Ai.Delivery.Examples;

using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Resolution;
using Kontent.Ai.Delivery.Extensions;
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
            foreach (var item in result.Value)
            {
                Console.WriteLine($"- {item.System.Name}");
            }
        }
    }

    // Items feed pagination
    public static async Task ItemsFeedAsync(IDeliveryClient client)
    {
        var query = client.GetItemsFeed()
            .OrderBy(ItemSystemPath.LastModified, true);

        await foreach (var item in query.EnumerateItemsAsync())
        {
            Console.WriteLine($"Item: {item.System.Name}");
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
            .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
            .Filter(f => f.Contains(Elements.GetPath("title"), "coffee"))
            .Limit(20)
            .ExecuteAsync();

        _ = result;
    }

    // Using .Where with preconstructed filters - not available publicly (Filter is internal). Commented out for later fix.
    public static async Task UsingWhereWithPreconstructedFilterAsync(IDeliveryClient client)
    {

        var filter = new Filter(
            ItemSystemPath.Type,
            FilterOperator.Equals,
            StringValue.From("article"));

        var result = await client.GetItems()
            .Where(filter)
            .ExecuteAsync();

        await Task.CompletedTask;
    }

    // Common filter operators
    public static async Task CommonFilterOperatorsAsync(IDeliveryClient client)
    {
        var result = await client.GetItems()
            .Filter(f => f.Equals(ItemSystemPath.Type, "product"))
            .Filter(f => f.NotEquals(ItemSystemPath.Collection, "archived"))
            .Filter(f => f.GreaterThan(Elements.GetPath("price"), 100.0))
            .Filter(f => f.LessThanOrEqual(Elements.GetPath("rating"), 4.5))
            .Filter(f => f.Range(Elements.GetPath("price"), (50.0, 500.0)))
            .Filter(f => f.In(ItemSystemPath.Type, new[] { "article", "blog_post" }))
            .Filter(f => f.Any(Elements.GetPath("tags"), "featured", "trending"))
            .Filter(f => f.All(Elements.GetPath("categories"), "tech", "news"))
            .Filter(f => f.NotEmpty(Elements.GetPath("description")))
            .Filter(f => f.In(ItemSystemPath.Collection, ["tech", "news"]))
            .ExecuteAsync();

        _ = result;
    }

    // Ordering and pagination
    public static async Task OrderingAndPaginationAsync(IDeliveryClient client)
    {
        var result = await client.GetItems()
            .OrderBy(ItemSystemPath.LastModified, ascending: false)
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
    public record Article : IElementsModel
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public RichTextContent BodyCopy { get; set; } = [];
        public DateTime PublishDate { get; set; }
        public IEnumerable<IEmbeddedContent>? RelatedArticles { get; set; }
    }

    public record Product : IElementsModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public record Video : IElementsModel
    {
        public string Title { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
    }

    public record HomePage : IElementsModel
    {
        public IEnumerable<IEmbeddedContent> FeaturedContent { get; set; } = Array.Empty<IEmbeddedContent>();
    }

    public record Author(string Name);

    // Query with strong typing
    public static async Task StrongTypingQueryAsync(IDeliveryClient client)
    {
        var result = await client.GetItems<Article>()
            .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
            .WithLanguage("en-US")
            .ExecuteAsync();

        if (result.IsSuccess)
        {
            foreach (var article in result.Value)
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
                // Access metadata for all types
                Console.WriteLine($"Type: {linkedItem.ContentTypeCodename}");
                Console.WriteLine($"Codename: {linkedItem.Codename}");
                Console.WriteLine($"Name: {linkedItem.Name}");
                Console.WriteLine($"ID: {linkedItem.Id}");

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
                        Console.WriteLine($"Unknown type: {item.ContentTypeCodename}");
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
            .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
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
            foreach (var language in result.Value)
            {
                Console.WriteLine($"{language.System.Name} ({language.System.Codename})");
            }
        }
    }

    // Memory cache configuration
    public static void MemoryCacheConfig(IServiceCollection services)
    {
        services.AddDeliveryClient(options =>
        {
            options.EnvironmentId = "your-environment-id";
        })
        .WithMemoryCache(defaultExpiration: TimeSpan.FromHours(1));
    }

    // Distributed cache configuration (requires distributed cache registered; keep as demo)
    public static void DistributedCacheConfig(IServiceCollection services)
    {
        services.AddDeliveryClient(options =>
        {
            options.EnvironmentId = "your-environment-id";
        })
        .WithDistributedCache(defaultExpiration: TimeSpan.FromHours(2));
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

    // Simple usage with environment ID
    public static IDeliveryClient BuilderSimpleUsage()
    {
        var client = DeliveryClientBuilder
            .WithEnvironmentId("your-environment-id")
            .Build();

        return client;
    }

    // Using environment ID as Guid
    public static IDeliveryClient BuilderWithGuidEnvironmentId()
    {
        var client = DeliveryClientBuilder
            .WithEnvironmentId(Guid.Parse("550cec62-90a6-4ab3-b3e4-3d0bb4c04f5c"))
            .Build();

        return client;
    }

    // With custom type provider
    public static IDeliveryClient BuilderWithTypeProvider(ITypeProvider typeProvider)
    {
        var client = DeliveryClientBuilder
            .WithEnvironmentId("your-environment-id")
            .WithTypeProvider(typeProvider)
            .Build();

        return client;
    }

    // With memory cache
    public static IDeliveryClient BuilderWithMemoryCache()
    {
        var client = DeliveryClientBuilder
            .WithEnvironmentId("your-environment-id")
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        return client;
    }

    // With distributed cache
    public static IDeliveryClient BuilderWithDistributedCache(IDistributedCache distributedCache)
    {
        var client = DeliveryClientBuilder
            .WithEnvironmentId("your-environment-id")
            .WithDistributedCache(distributedCache, TimeSpan.FromHours(1))
            .Build();

        return client;
    }

    // With full options using WithOptions
    public static IDeliveryClient BuilderWithFullOptions()
    {
        var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId("your-environment-id")
                .UsePreviewApi("your-preview-api-key")
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(15))
            .Build();

        return client;
    }

    // Combined: type provider + memory cache
    public static IDeliveryClient BuilderFullExample(ITypeProvider typeProvider)
    {
        var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId("your-environment-id")
                .UseProductionApi()
                .WithDefaultRenditionPreset("mobile")
                .Build())
            .WithTypeProvider(typeProvider)
            .WithMemoryCache(TimeSpan.FromHours(1))
            .Build();

        return client;
    }
}
