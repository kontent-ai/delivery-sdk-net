using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kontent.Ai.Delivery.Tests.QueryBuilders;

/// <summary>
/// Tests for query parameter handling including depth, elements projection,
/// and the WaitForLoadingNewContent header.
/// </summary>
public sealed class QueryParameterTests
{
    private readonly string _env = Guid.NewGuid().ToString();
    private string BaseUrl => $"https://deliver.kontent.ai/{_env}";

    private IDeliveryClient CreateClient(
        MockHttpMessageHandler mockHttp,
        DeliveryOptions? options = null,
        ITypeProvider? typeProvider = null)
    {
        var services = new ServiceCollection();
        if (typeProvider is not null)
        {
            services.AddSingleton(typeProvider);
        }

        var opts = options ?? new DeliveryOptions { EnvironmentId = _env };
        services.AddDeliveryClient(opts, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    #region Depth Parameter Tests

    [Fact]
    public async Task GetItem_WithDepth_AddsDepthQueryParameter()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemJson = BuildMinimalItemJson("test_item");

        mock.When($"{BaseUrl}/items/test_item")
            .With(req => req.RequestUri!.Query.Contains("depth=3"))
            .Respond("application/json", itemJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<IDynamicElements>("test_item")
            .Depth(3)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
    }

    [Fact]
    public async Task GetItems_WithDepth_AddsDepthQueryParameter()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemsJson = BuildMinimalItemsListingJson(["item_1", "item_2"]);

        mock.When($"{BaseUrl}/items")
            .With(req => req.RequestUri!.Query.Contains("depth=2"))
            .Respond("application/json", itemsJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItems<IDynamicElements>()
            .Depth(2)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
    }

    [Fact]
    public async Task GetItem_WithDepthZero_AddsDepthQueryParameter()
    {
        // Arrange - Depth 0 means no linked items are resolved
        var mock = new MockHttpMessageHandler();
        var itemJson = BuildMinimalItemJson("test_item");

        mock.When($"{BaseUrl}/items/test_item")
            .With(req => req.RequestUri!.Query.Contains("depth=0"))
            .Respond("application/json", itemJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<IDynamicElements>("test_item")
            .Depth(0)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
    }

    #endregion

    #region Elements Projection Tests

    [Fact]
    public async Task GetItem_WithElements_SerializesAsSingleCommaSeparatedParameter()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemJson = BuildMinimalItemJson("test_item");
        string? capturedQuery = null;

        mock.When($"{BaseUrl}/items/test_item")
            .With(req =>
            {
                capturedQuery = req.RequestUri!.Query;
                return true;
            })
            .Respond("application/json", itemJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<IDynamicElements>("test_item")
            .WithElements("title", "summary")
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
        Assert.NotNull(capturedQuery);
        // Must be a single comma-delimited value, not repeated elements= keys.
        Assert.Contains("elements=title%2Csummary", capturedQuery);
        Assert.Equal(1, CountOccurrences(capturedQuery, "elements="));
    }

    [Fact]
    public async Task GetItem_WithoutElements_SerializesAsSingleCommaSeparatedParameter()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemJson = BuildMinimalItemJson("test_item");
        string? capturedQuery = null;

        mock.When($"{BaseUrl}/items/test_item")
            .With(req =>
            {
                capturedQuery = req.RequestUri!.Query;
                return true;
            })
            .Respond("application/json", itemJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<IDynamicElements>("test_item")
            .WithoutElements("body_copy", "metadata")
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
        Assert.NotNull(capturedQuery);
        Assert.Contains("excludeElements=body_copy%2Cmetadata", capturedQuery);
        Assert.Equal(1, CountOccurrences(capturedQuery, "excludeElements="));
    }

    [Fact]
    public async Task GetItems_WithElements_SerializesAsSingleCommaSeparatedParameter()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemsJson = BuildMinimalItemsListingJson(["item_1"]);
        string? capturedQuery = null;

        mock.When($"{BaseUrl}/items")
            .With(req =>
            {
                capturedQuery = req.RequestUri!.Query;
                return true;
            })
            .Respond("application/json", itemsJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItems<IDynamicElements>()
            .WithElements("title", "summary", "body")
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
        Assert.NotNull(capturedQuery);
        Assert.Contains("elements=title%2Csummary%2Cbody", capturedQuery);
        Assert.Equal(1, CountOccurrences(capturedQuery, "elements="));
    }

    [Fact]
    public async Task GetItems_WithSingleElement_SerializesWithoutComma()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemsJson = BuildMinimalItemsListingJson(["item_1"]);
        string? capturedQuery = null;

        mock.When($"{BaseUrl}/items")
            .With(req =>
            {
                capturedQuery = req.RequestUri!.Query;
                return true;
            })
            .Respond("application/json", itemsJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItems<IDynamicElements>()
            .WithElements("title")
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
        Assert.NotNull(capturedQuery);
        Assert.Contains("elements=title", capturedQuery);
        Assert.DoesNotContain("%2C", capturedQuery); // no comma for single element
    }

    #endregion

    #region WaitForLoadingNewContent Tests

    [Fact]
    public async Task GetItem_WaitForLoadingNewContent_AddsHeader()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemJson = BuildMinimalItemJson("test_item");

        mock.When($"{BaseUrl}/items/test_item")
            .With(req => req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond("application/json", itemJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<IDynamicElements>("test_item")
            .WaitForLoadingNewContent()
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
    }

    [Fact]
    public async Task GetItems_WaitForLoadingNewContent_AddsHeader()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemsJson = BuildMinimalItemsListingJson(["item_1"]);

        mock.When($"{BaseUrl}/items")
            .With(req => req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond("application/json", itemsJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItems<IDynamicElements>()
            .WaitForLoadingNewContent()
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
    }

    [Fact]
    public async Task GetItem_WithoutWaitForLoadingNewContent_DoesNotAddHeader()
    {
        // Arrange - Default behavior without the header
        var mock = new MockHttpMessageHandler();
        var itemJson = BuildMinimalItemJson("test_item");

        mock.When($"{BaseUrl}/items/test_item")
            .With(req => !req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond("application/json", itemJson);

        var client = CreateClient(mock);

        // Act - Don't call WaitForLoadingNewContent
        var result = await client.GetItem<IDynamicElements>("test_item").ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
    }

    [Fact]
    public async Task GetItem_WaitForLoadingNewContentFalse_OmitsHeader()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemJson = BuildMinimalItemJson("test_item");

        mock.When($"{BaseUrl}/items/test_item")
            .With(req => !req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond("application/json", itemJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<IDynamicElements>("test_item")
            .WaitForLoadingNewContent(false)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
    }

    [Fact]
    public async Task GetContentElement_WaitForLoadingNewContent_AddsHeader()
    {
        var mock = new MockHttpMessageHandler();

        mock.When($"{BaseUrl}/types/article/elements/title")
            .With(req => req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond("application/json", "{\"type\":\"text\",\"name\":\"Title\",\"codename\":\"title\"}");

        var client = CreateClient(mock);

        var result = await client.GetContentElement("article", "title")
            .WaitForLoadingNewContent()
            .ExecuteAsync();

        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
    }

    [Fact]
    public async Task GetItems_GenericExecuteTwice_DoesNotDuplicateAutoTypeFilter()
    {
        // Arrange - repeated execution on the same query should keep a stable system.type filter
        var mock = new MockHttpMessageHandler();
        var itemsJson = BuildMinimalItemsListingJson(["item_1"]);
        const string typeFilterKey = "system.type%5Beq%5D";

        mock.When($"{BaseUrl}/items")
            .With(req => CountOccurrences(req.RequestUri!.Query, typeFilterKey) == 1)
            .Respond("application/json", itemsJson);

        var client = CreateClient(mock, typeProvider: new StaticTypeProvider());
        var query = client.GetItems<Article>();

        // Act
        var firstResult = await query.ExecuteAsync();
        var secondResult = await query.ExecuteAsync();

        // Assert
        Assert.True(firstResult.IsSuccess, $"Request failed: {firstResult.Error?.Message}");
        Assert.True(secondResult.IsSuccess, $"Request failed: {secondResult.Error?.Message}");
    }

    #endregion

    #region Combined Parameters Tests

    [Fact]
    public async Task GetItems_CombinedParameters_AllPreservedInQuery()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemsJson = BuildMinimalItemsListingJson(["item_1", "item_2"]);
        string? capturedQuery = null;

        mock.When($"{BaseUrl}/items")
            .With(req =>
            {
                capturedQuery = req.RequestUri!.Query;
                return req.Headers.Contains("X-KC-Wait-For-Loading-New-Content");
            })
            .Respond("application/json", itemsJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItems<IDynamicElements>()
            .Depth(2)
            .WithElements("title", "summary")
            .Limit(10)
            .Where(f => f.System("type").IsEqualTo("article"))
            .WaitForLoadingNewContent()
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
        Assert.NotNull(capturedQuery);
        Assert.Contains("depth=2", capturedQuery);
        Assert.Contains("elements=title%2Csummary", capturedQuery);
        Assert.Contains("limit=10", capturedQuery);
        Assert.Equal(1, CountOccurrences(capturedQuery, "elements="));
    }

    [Fact]
    public async Task GetItem_WithLanguageAndDepthAndElements_AllPreserved()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var itemJson = BuildMinimalItemJson("test_item");
        string? capturedQuery = null;

        mock.When($"{BaseUrl}/items/test_item")
            .With(req =>
            {
                capturedQuery = req.RequestUri!.Query;
                return true;
            })
            .Respond("application/json", itemJson);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<IDynamicElements>("test_item")
            .WithLanguage("es-ES")
            .Depth(1)
            .WithElements("title")
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
        Assert.NotNull(capturedQuery);
        Assert.Contains("language=es-ES", capturedQuery);
        Assert.Contains("depth=1", capturedQuery);
        Assert.Contains("elements=title", capturedQuery);
        Assert.Equal(1, CountOccurrences(capturedQuery, "elements="));
    }

    [Fact]
    public async Task GetLanguages_OrderByDescending_SerializesOrderParameter()
    {
        var mock = new MockHttpMessageHandler();
        string? capturedQuery = null;

        mock.When($"{BaseUrl}/languages")
            .With(req =>
            {
                capturedQuery = req.RequestUri!.Query;
                return true;
            })
            .Respond("application/json", BuildMinimalLanguagesListingJson(["en-US"]));

        var client = CreateClient(mock);

        var result = await client.GetLanguages()
            .OrderBy("system.name", OrderingMode.Descending)
            .ExecuteAsync();

        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
        Assert.NotNull(capturedQuery);
        Assert.Contains("order=system.name%5Bdesc%5D", capturedQuery);
    }

    [Fact]
    public async Task GetLanguages_OrderByAscending_SerializesOrderParameter()
    {
        var mock = new MockHttpMessageHandler();
        string? capturedQuery = null;

        mock.When($"{BaseUrl}/languages")
            .With(req =>
            {
                capturedQuery = req.RequestUri!.Query;
                return true;
            })
            .Respond("application/json", BuildMinimalLanguagesListingJson(["en-US"]));

        var client = CreateClient(mock);

        var result = await client.GetLanguages()
            .OrderBy("system.name", OrderingMode.Ascending)
            .ExecuteAsync();

        Assert.True(result.IsSuccess, $"Request failed: {result.Error?.Message}");
        Assert.NotNull(capturedQuery);
        Assert.Contains("order=system.name%5Basc%5D", capturedQuery);
    }

    #endregion

    #region Helper Methods

    private static string BuildMinimalItemJson(string codename)
    {
        return $$"""
            {
                "item": {
                    "system": {
                        "id": "{{Guid.NewGuid()}}",
                        "name": "{{codename}}",
                        "codename": "{{codename}}",
                        "language": "default",
                        "type": "article",
                        "collection": "default",
                        "last_modified": "2024-01-01T00:00:00Z"
                    },
                    "elements": {}
                },
                "modular_content": {}
            }
            """;
    }

    private static string BuildMinimalItemsListingJson(IReadOnlyList<string> codenames)
    {
        var itemsJson = string.Join(",", codenames.Select(codename => $$"""
            {
                "system": {
                    "id": "{{Guid.NewGuid()}}",
                    "name": "{{codename}}",
                    "codename": "{{codename}}",
                    "language": "default",
                    "type": "article",
                    "collection": "default",
                    "last_modified": "2024-01-01T00:00:00Z"
                },
                "elements": {}
            }
            """));

        return $$"""
            {
                "items": [{{itemsJson}}],
                "pagination": {
                    "skip": 0,
                    "limit": 100,
                    "count": {{codenames.Count}},
                    "next_page": ""
                },
                "modular_content": {}
            }
            """;
    }

    private static string BuildMinimalLanguagesListingJson(IReadOnlyList<string> codenames)
    {
        var languagesJson = string.Join(",", codenames.Select(codename => $$"""
            {
                "system": {
                    "id": "{{Guid.NewGuid()}}",
                    "name": "{{codename}}",
                    "codename": "{{codename}}"
                }
            }
            """));

        return $$"""
            {
                "languages": [{{languagesJson}}],
                "pagination": {
                    "skip": 0,
                    "limit": 100,
                    "count": {{codenames.Count}},
                    "next_page": ""
                }
            }
            """;
    }

    private static int CountOccurrences(string input, string value)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var count = 0;
        var index = 0;
        while ((index = input.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private sealed class StaticTypeProvider : ITypeProvider
    {
        public Type? GetType(string contentType)
            => string.Equals(contentType, "article", StringComparison.OrdinalIgnoreCase)
                ? typeof(Article)
                : null;

        public string? GetCodename(Type contentType)
            => contentType == typeof(Article) ? "article" : null;
    }

    #endregion
}
