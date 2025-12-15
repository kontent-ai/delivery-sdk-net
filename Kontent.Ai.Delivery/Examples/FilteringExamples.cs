using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Examples;

/// <summary>
/// Comprehensive examples demonstrating the type-safe filtering capabilities
/// of the Kontent.ai Delivery SDK.
/// </summary>
/// <inheritdoc/>
public class FilteringExamples(IDeliveryClient client)
{
    private readonly IDeliveryClient _client = client;

    /// <summary>
    /// Example 1: Basic filtering on system properties.
    /// </summary>
    public async Task BasicSystemPropertyFiltering()
    {
        // Filter by content type
        var articles = await _client.GetItems<Article>()
            .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
            .ExecuteAsync();

        // Filter by multiple system properties
        var recentEnglishArticles = await _client.GetItems<Article>()
            .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
            .Filter(f => f.Equals(ItemSystemPath.Language, "en-US"))
            .Filter(f => f.GreaterThan(ItemSystemPath.LastModified, DateTime.UtcNow.AddDays(-30)))
            .ExecuteAsync();

        // Filter by content item ID
        var specificItem = await _client.GetItems<ContentItem>()
            .Filter(f => f.Equals(ItemSystemPath.Id, "f4b3fc05-e988-4dae-9ac1-a94aba566474"))
            .ExecuteAsync();

        // Filter by collection
        var newsItems = await _client.GetItems<ContentItem>()
            .Filter(f => f.Equals(ItemSystemPath.Collection, "news"))
            .ExecuteAsync();
    }

    /// <summary>
    /// Example 2: Element property filtering with various operators.
    /// </summary>
    public async Task ElementPropertyFiltering()
    {
        // String contains operator
        var coffeeArticles = await _client.GetItems<Article>()
            .Filter(f => f.Contains(Elements.GetPath("title"), "coffee"))
            .ExecuteAsync();

        // Numeric comparison operators
        var expensiveProducts = await _client.GetItems<Product>()
            .Filter(f => f.GreaterThanOrEqual(Elements.GetPath("price"), 1000))
            .ExecuteAsync();

        var affordableProducts = await _client.GetItems<Product>()
            .Filter(f => f.LessThan(Elements.GetPath("price"), 100))
            .ExecuteAsync();

        // Range operator for numeric values
        var midRangeProducts = await _client.GetItems<Product>()
            .Filter(f => f.Range(Elements.GetPath("price"), (100, 500)))
            .ExecuteAsync();

        // Boolean filtering
        var featuredProducts = await _client.GetItems<Product>()
            .Filter(f => f.Equals(Elements.GetPath("is_featured"), true))
            .ExecuteAsync();

        // Empty/NotEmpty operators
        var itemsWithDescription = await _client.GetItems<ContentItem>()
            .Filter(f => f.NotEmpty(Elements.GetPath("description")))
            .ExecuteAsync();

        var itemsWithoutImage = await _client.GetItems<ContentItem>()
            .Filter(f => f.Empty(Elements.GetPath("thumbnail")))
            .ExecuteAsync();
    }

    /// <summary>
    /// Example 3: Array operators for multi-value filtering.
    /// </summary>
    public async Task ArrayOperatorFiltering()
    {
        // IN operator - matches any of the specified values
        var techArticles = await _client.GetItems<Article>()
            .Filter(f => f.In(Elements.GetPath("category"),
                ["technology", "programming", "software"]))
            .ExecuteAsync();

        // NOT IN operator - excludes specified values
        var nonDraftItems = await _client.GetItems<ContentItem>()
            .Filter(f => f.NotIn(ItemSystemPath.WorkflowStep,
                ["draft", "review", "archived"]))
            .ExecuteAsync();

        // ANY operator - array contains any of the specified values
        var taggedArticles = await _client.GetItems<Article>()
            .Filter(f => f.Any(Elements.GetPath("tags"),
                "featured", "trending", "popular"))
            .ExecuteAsync();

        // ALL operator - array contains all specified values
        var completeProducts = await _client.GetItems<Product>()
            .Filter(f => f.All(Elements.GetPath("required_features"),
                "warranty", "manual", "support"))
            .ExecuteAsync();

        // Multiple values in system properties
        var selectedTypes = await _client.GetItems<ContentItem>()
            .Filter(f => f.In(ItemSystemPath.Type,
                ["article", "blog_post", "news"]))
            .ExecuteAsync();
    }

    /// <summary>
    /// Example 4: Date and time filtering.
    /// </summary>
    public async Task DateTimeFiltering()
    {
        var now = DateTime.UtcNow;

        // Items modified in the last week
        var recentItems = await _client.GetItems<ContentItem>()
            .Filter(f => f.GreaterThan(ItemSystemPath.LastModified, now.AddDays(-7)))
            .ExecuteAsync();

        // Items modified between two dates
        var dateRangeItems = await _client.GetItems<ContentItem>()
            .Filter(f => f.Range(ItemSystemPath.LastModified,
                (now.AddMonths(-3), now.AddMonths(-1))))
            .ExecuteAsync();

        // Events happening in the future
        var upcomingEvents = await _client.GetItems<Event>()
            .Filter(f => f.GreaterThan(Elements.GetPath("event_date"), now))
            .ExecuteAsync();

        // Articles published this year
        var thisYearArticles = await _client.GetItems<Article>()
            .Filter(f => f.GreaterThanOrEqual(Elements.GetPath("publish_date"),
                new DateTime(now.Year, 1, 1)))
            .Filter(f => f.LessThan(Elements.GetPath("publish_date"),
                new DateTime(now.Year + 1, 1, 1)))
            .ExecuteAsync();
    }

    /// <summary>
    /// Example 5: Complex multi-filter scenarios.
    /// </summary>
    public async Task ComplexFilteringScenarios()
    {
        // E-commerce product filtering
        var filteredProducts = await _client.GetItems<Product>()
            .Filter(f => f.Equals(ItemSystemPath.Type, "product"))
            .Filter(f => f.Range(Elements.GetPath("price"), (50, 500)))
            .Filter(f => f.GreaterThanOrEqual(Elements.GetPath("rating"), 4.0))
            .Filter(f => f.Any(Elements.GetPath("categories"),
                "electronics", "computers", "accessories"))
            .Filter(f => f.NotEmpty(Elements.GetPath("description")))
            .Filter(f => f.Equals(Elements.GetPath("in_stock"), true))
            .WithLanguage("en-US")
            .Limit(20)
            .OrderBy("elements.rating", false) // Descending
            .ExecuteAsync();

        // Blog post filtering with multiple criteria
        var qualityBlogPosts = await _client.GetItems<BlogPost>()
            .Filter(f => f.Equals(ItemSystemPath.Type, "blog_post"))
            .Filter(f => f.GreaterThan(ItemSystemPath.LastModified,
                DateTime.UtcNow.AddMonths(-6)))
            .Filter(f => f.Contains(Elements.GetPath("title"), "tutorial"))
            .Filter(f => f.All(Elements.GetPath("tags"),
                "verified", "complete"))
            .Filter(f => f.NotEquals(Elements.GetPath("status"), "draft"))
            .Filter(f => f.GreaterThan(Elements.GetPath("word_count"), 1000))
            .WithTotalCount()
            .ExecuteAsync();

        // Content audit query
        var outdatedContent = await _client.GetItems<ContentItem>()
            .Filter(f => f.LessThan(ItemSystemPath.LastModified,
                DateTime.UtcNow.AddYears(-2)))
            .Filter(f => f.Empty(Elements.GetPath("seo_description")))
            .Filter(f => f.NotIn(ItemSystemPath.Type,
                ["navigation", "settings", "configuration"]))
            .Filter(f => f.NotEquals(ItemSystemPath.Collection, "archive"))
            .ExecuteAsync();
    }

    /// <summary>
    /// Example 6: Type-specific filtering (limited capabilities).
    /// </summary>
    public async Task ContentTypeFiltering()
    {
        // Basic type filtering
        var articleType = await _client.GetTypes()
            .Filter(f => f.Equals(TypeSystemPath.Codename, "article"))
            .ExecuteAsync();

        // Multiple types
        var contentTypes = await _client.GetTypes()
            .Filter(f => f.In(TypeSystemPath.Codename,
                "article", "blog_post", "news"))
            .ExecuteAsync();

        // Types modified recently
        var recentlyModifiedTypes = await _client.GetTypes()
            .Filter(f => f.GreaterThan(TypeSystemPath.LastModified,
                DateTime.UtcNow.AddMonths(-3)))
            .ExecuteAsync();

        // Types within date range
        var typesInRange = await _client.GetTypes()
            .Filter(f => f.Range(TypeSystemPath.LastModified,
                DateTime.Parse("2024-01-01"),
                DateTime.Parse("2024-06-30")))
            .ExecuteAsync();

        // Note: Types endpoint has limited filtering support
        // The following would NOT compile (compile-time safety):
        // .Where(f => f.Contains(Elements.GetPath("name"), "text")) // ❌ Elements not supported
        // .Where(f => f.All(TypeSystemPath.Codename, "a", "b"))    // ❌ Collection operators not supported
    }

    /// <summary>
    /// Example 7: Taxonomy filtering (very limited capabilities).
    /// </summary>
    public async Task TaxonomyFiltering()
    {
        // Basic taxonomy filtering
        var categoriesTaxonomy = await _client.GetTaxonomies()
            .Where(f => f.Equals(TaxonomySystemPath.Codename, "categories"))
            .ExecuteAsync();

        // Exclude specific taxonomy
        var nonInternalTaxonomies = await _client.GetTaxonomies()
            .Where(f => f.NotEquals(TaxonomySystemPath.Codename, "internal"))
            .ExecuteAsync();

        // Date equality (exact match only for taxonomies)
        var specificDateTaxonomy = await _client.GetTaxonomies()
            .Where(f => f.Equals(TaxonomySystemPath.LastModified,
                DateTime.Parse("2024-08-14T10:30:00Z")))
            .ExecuteAsync();

        // Note: Taxonomies endpoint has very limited filtering support
        // The following would NOT compile (compile-time safety):
        // .Where(f => f.Range(TaxonomySystemPath.LastModified, from, to)) // ❌ Range not supported
        // .Where(f => f.In(TaxonomySystemPath.Codename, "a", "b"))       // ❌ IN operator not supported
        // .Where(f => f.GreaterThan(TaxonomySystemPath.LastModified, date)) // ❌ Comparison operators not supported
    }

    /// <summary>
    /// Example 8: Combining filters with other query parameters.
    /// </summary>
    public async Task CombiningFiltersWithOtherParameters()
    {
        // Full query with filters and all other parameters
        var comprehensiveQuery = await _client.GetItems<Article>()
            // Filters
            .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
            .Filter(f => f.Contains(Elements.GetPath("title"), "guide"))
            .Filter(f => f.Any(Elements.GetPath("tags"), "beginner", "intermediate"))
            // Other query parameters
            .WithLanguage("en-US")
            .WithElements("title", "summary", "content", "author")
            .Depth(2)
            .Skip(20)
            .Limit(10)
            .OrderBy("system.last_modified", false)
            .WithTotalCount()
            .ExecuteAsync();

        // Pagination with filters
        const int pageSize = 25;
        for (int page = 0; page < 5; page++)
        {
            var pagedResults = await _client.GetItems<Product>()
                .Filter(f => f.Equals(Elements.GetPath("category"), "electronics"))
                .Filter(f => f.GreaterThan(Elements.GetPath("price"), 100))
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ExecuteAsync();

            // Process page results...
        }
    }

    /// <summary>
    /// Example 9: Direct filter creation (advanced usage).
    /// </summary>
    public async Task DirectFilterCreation()
    {
        // Sometimes you might want to create filters directly
        var customFilter = new Filter(
            ItemSystemPath.Type,
            FilterOperator.Equals,
            StringValue.From("article")
        );

        // Use the direct filter
        var results = await _client.GetItems<Article>()
            .Where(customFilter)
            .ExecuteAsync();

        // Create complex value filters
        var rangeFilter = new Filter(
            Elements.GetPath("price"),
            FilterOperator.Range,
            NumericRangeValue.From((100, 500))
        );

        var arrayFilter = new Filter(
            Elements.GetPath("tags"),
            FilterOperator.In,
            StringArrayValue.From(["featured", "popular", "trending"])
        );

        // Apply multiple direct filters
        var complexResults = await _client.GetItems<Product>()
            .Where(rangeFilter)
            .Where(arrayFilter)
            .ExecuteAsync();
    }

    /// <summary>
    /// Example 10: Special characters and encoding.
    /// </summary>
    public async Task SpecialCharacterHandling()
    {
        // Filters handle special characters automatically
        var specialChars = await _client.GetItems<Article>()
            .Filter(f => f.Contains(Elements.GetPath("title"), "coffee & tea"))
            .ExecuteAsync();
        // Generates: elements.title[contains]=coffee%20%26%20tea

        // Unicode characters
        var unicodeSearch = await _client.GetItems<Article>()
            .Filter(f => f.Contains(Elements.GetPath("title"), "café naïve"))
            .ExecuteAsync();
        // Properly URL-encoded in the query string

        // Multiple values with special characters
        var specialCategories = await _client.GetItems<Product>()
            .Filter(f => f.In(Elements.GetPath("category"),
                ["home & garden", "sports & outdoors", "arts & crafts"]))
            .ExecuteAsync();
        // Each value is properly encoded in the comma-separated list

        // Collection expressions with string arrays
        var modernSyntax = await _client.GetItems<Product>()
            .Filter(f => f.In(ItemSystemPath.Type, ["product", "variant", "bundle"]))
            .ExecuteAsync();

        // Collection expressions with numeric arrays
        var ratedItems = await _client.GetItems<Product>()
            .Filter(f => f.In(Elements.GetPath("rating"), [4.0, 5.0]))
            .ExecuteAsync();
    }
}
/// <inheritdoc/>

// Example model classes (these would typically be generated)
// Models are now plain POCOs - no interface required!
public class Article { }
public class Product { }
public class BlogPost { }
public class Event { }
public class ContentItem { }