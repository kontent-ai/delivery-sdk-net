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
            .Filter(f => f.System("type").Eq("article"))
            .ExecuteAsync();

        // Filter by multiple system properties (multiple Filter calls)
        var recentEnglishArticles = await _client.GetItems<Article>()
            .Filter(f => f
                .System("type").Eq("article")
                .System("language").Eq("en-US")
                .System("last_modified").Gt(DateTime.UtcNow.AddDays(-30)))
            .ExecuteAsync();

        // Filter by multiple system properties (single Filter call with chaining)
        var recentEnglishArticlesChained = await _client.GetItems<Article>()
            .Filter(f => f
                .System("type").Eq("article")
                .System("language").Eq("en-US")
                .System("last_modified").Gt(DateTime.UtcNow.AddDays(-30)))
            .ExecuteAsync();

        // Filter by content item ID
        var specificItem = await _client.GetItems<ContentItem>()
            .Filter(f => f.System("id").Eq("f4b3fc05-e988-4dae-9ac1-a94aba566474"))
            .ExecuteAsync();

        // Filter by collection
        var newsItems = await _client.GetItems<ContentItem>()
            .Filter(f => f.System("collection").Eq("news"))
            .ExecuteAsync();

        _ = recentEnglishArticlesChained;
    }

    /// <summary>
    /// Example 2: Element property filtering with various operators.
    /// </summary>
    public async Task ElementPropertyFiltering()
    {
        var coffeeArticle = await _client.GetItems<Article>()
            .Filter(f => f.Element("title").Eq("Coffee"))
            .ExecuteAsync();

        // Numeric comparison operators
        var expensiveProducts = await _client.GetItems<Product>()
            .Filter(f => f.Element("price").Gte(1000))
            .ExecuteAsync();

        var affordableProducts = await _client.GetItems<Product>()
            .Filter(f => f.Element("price").Lt(100))
            .ExecuteAsync();

        // Range operator for numeric values
        var midRangeProducts = await _client.GetItems<Product>()
            .Filter(f => f.Element("price").Range(100, 500))
            .ExecuteAsync();

        // Boolean filtering
        var featuredProducts = await _client.GetItems<Product>()
            .Filter(f => f.Element("is_featured").Eq(true))
            .ExecuteAsync();

        // Empty/NotEmpty operators
        var itemsWithDescription = await _client.GetItems<ContentItem>()
            .Filter(f => f.Element("description").Nempty())
            .ExecuteAsync();

        var itemsWithoutImage = await _client.GetItems<ContentItem>()
            .Filter(f => f.Element("thumbnail").Empty())
            .ExecuteAsync();
    }

    /// <summary>
    /// Example 3: Array operators for multi-value filtering.
    /// </summary>
    public async Task ArrayOperatorFiltering()
    {
        // IN operator - matches any of the specified values
        var techArticles = await _client.GetItems<Article>()
            .Filter(f => f.Element("category").In("technology", "programming", "software"))
            .ExecuteAsync();

        // NOT IN operator - excludes specified values
        var nonDraftItems = await _client.GetItems<ContentItem>()
            .Filter(f => f.System("workflow_step").Nin("draft", "review", "archived"))
            .ExecuteAsync();

        // ANY operator - array contains any of the specified values
        var taggedArticles = await _client.GetItems<Article>()
            .Filter(f => f.Element("tags").Any("featured", "trending", "popular"))
            .ExecuteAsync();

        // ALL operator - array contains all specified values
        var completeProducts = await _client.GetItems<Product>()
            .Filter(f => f.Element("required_features").All("warranty", "manual", "support"))
            .ExecuteAsync();

        // Multiple values in system properties
        var selectedTypes = await _client.GetItems<ContentItem>()
            .Filter(f => f.System("type").In("article", "blog_post", "news"))
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
            .Filter(f => f.System("last_modified").Gt(now.AddDays(-7)))
            .ExecuteAsync();

        // Items modified between two dates
        var dateRangeItems = await _client.GetItems<ContentItem>()
            .Filter(f => f.System("last_modified").Range(now.AddMonths(-3), now.AddMonths(-1)))
            .ExecuteAsync();

        // Events happening in the future
        var upcomingEvents = await _client.GetItems<Event>()
            .Filter(f => f.Element("event_date").Gt(now))
            .ExecuteAsync();

        // Articles published this year
        var thisYearArticles = await _client.GetItems<Article>()
            .Filter(f => f
                .Element("publish_date").Gte(new DateTime(now.Year, 1, 1))
                .Element("publish_date").Lt(new DateTime(now.Year + 1, 1, 1)))
            .ExecuteAsync();
    }

    /// <summary>
    /// Example 5: Complex multi-filter scenarios.
    /// </summary>
    public async Task ComplexFilteringScenarios()
    {
        // E-commerce product filtering
        var filteredProducts = await _client.GetItems<Product>()
            .Filter(f => f
                .System("type").Eq("product")
                .Element("price").Range(50, 500)
                .Element("rating").Gte(4.0)
                .Element("categories").Any("electronics", "computers", "accessories")
                .Element("description").Nempty()
                .Element("in_stock").Eq(true))
            .WithLanguage("en-US")
            .Limit(20)
            .OrderBy("elements.rating", false) // Descending
            .ExecuteAsync();

        // Blog post filtering with multiple criteria
        var qualityBlogPosts = await _client.GetItems<BlogPost>()
            .Filter(f => f
                .System("type").Eq("blog_post")
                .System("last_modified").Gt(DateTime.UtcNow.AddMonths(-6))
                .Element("tags").Any("tutorial")
                .Element("tags").All("verified", "complete")
                .Element("status").Neq("draft")
                .Element("word_count").Gt(1000))
            .WithTotalCount()
            .ExecuteAsync();

        var test = await _client.GetItems<Article>()
            .Filter(f => f.System("type").Eq("article"))
            .ExecuteAllAsync();

        // Content audit query
        var outdatedContent = await _client.GetItems<ContentItem>()
            .Filter(f => f
                .System("last_modified").Lt(DateTime.UtcNow.AddYears(-2))
                .Element("seo_description").Empty()
                .System("type").Nin("navigation", "settings", "configuration")
                .System("collection").Neq("archive"))
            .ExecuteAsync();
    }

    /// <summary>
    /// Example 6: Type-specific filtering (limited capabilities).
    /// </summary>
    public async Task ContentTypeFiltering()
    {
        // Basic type filtering
        var articleType = await _client.GetTypes()
            .Filter(f => f.System("codename").Eq("article"))
            .ExecuteAsync();

        // Multiple types
        var contentTypes = await _client.GetTypes()
            .Filter(f => f.System("codename").In("article", "blog_post", "news"))
            .ExecuteAsync();

        // Types modified recently
        var recentlyModifiedTypes = await _client.GetTypes()
            .Filter(f => f.System("last_modified").Gt(DateTime.UtcNow.AddMonths(-3)))
            .ExecuteAsync();

        // Types within date range
        var typesInRange = await _client.GetTypes()
            .Filter(f => f.System("last_modified").Range(DateTime.Parse("2024-01-01"), DateTime.Parse("2024-06-30")))
            .ExecuteAsync();

        // Note: Types endpoint has limited filtering support
        // The following would NOT compile (compile-time safety):
        // .Filter(f => f.Element("name").Contains("text"))        // ❌ Elements not supported on types
        // .Filter(f => f.System("codename").All("a", "b"))        // ❌ ALL operator not supported on types
    }

    /// <summary>
    /// Example 7: Taxonomy filtering (very limited capabilities).
    /// </summary>
    public async Task TaxonomyFiltering()
    {
        // Basic taxonomy filtering
        var categoriesTaxonomy = await _client.GetTaxonomies()
            .Filter(f => f.System("codename").Eq("categories"))
            .ExecuteAsync();

        // Exclude specific taxonomy
        var nonInternalTaxonomies = await _client.GetTaxonomies()
            .Filter(f => f.System("codename").Neq("internal"))
            .ExecuteAsync();

        // Date equality (exact match only for taxonomies)
        var specificDateTaxonomy = await _client.GetTaxonomies()
            .Filter(f => f.System("last_modified").Eq(DateTime.Parse("2024-08-14T10:30:00Z")))
            .ExecuteAsync();

        // Note: Taxonomies endpoint has very limited filtering support
        // The following would NOT compile (compile-time safety):
        // .Filter(f => f.System("last_modified").Range(from, to))        // ❌ Range not supported on taxonomies
        // .Filter(f => f.System("codename").In("a", "b"))                // ❌ IN operator not supported on taxonomies
        // .Filter(f => f.System("last_modified").Gt(date))               // ❌ Comparison operators not supported on taxonomies
    }

    /// <summary>
    /// Example 8: Combining filters with other query parameters.
    /// </summary>
    public async Task CombiningFiltersWithOtherParameters()
    {
        // Full query with filters and all other parameters
        var comprehensiveQuery = await _client.GetItems<Article>()
            // Filters
            .Filter(f => f
                .System("type").Eq("article")
                .Element("tags").Any("guide")
                .Element("tags").Any("beginner", "intermediate"))
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
                .Filter(f => f
                    .Element("category").Eq("electronics")
                    .Element("price").Gt(100))
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ExecuteAsync();

            // Process page results...
        }
    }

    /// <summary>
    /// Example 10: Special characters and encoding.
    /// </summary>
    public async Task SpecialCharacterHandling()
    {
        // Filters handle special characters automatically
        var specialChars = await _client.GetItems<Article>()
            .Filter(f => f.Element("title").Eq("coffee & tea"))
            .ExecuteAsync();
        // Generates: elements.title[eq]=coffee%20%26%20tea

        // Unicode characters
        var unicodeSearch = await _client.GetItems<Article>()
            .Filter(f => f.Element("title").Eq("café naïve"))
            .ExecuteAsync();
        // Properly URL-encoded in the query string

        // Multiple values with special characters
        var specialCategories = await _client.GetItems<Product>()
            .Filter(f => f.Element("category").In("home & garden", "sports & outdoors", "arts & crafts"))
            .ExecuteAsync();
        // Each value is properly encoded in the comma-separated list

        // Collection expressions with string arrays
        var modernSyntax = await _client.GetItems<Product>()
            .Filter(f => f.System("type").In("product", "variant", "bundle"))
            .ExecuteAsync();

        // Collection expressions with numeric arrays
        var ratedItems = await _client.GetItems<Product>()
            .Filter(f => f.Element("rating").In(4.0, 5.0))
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