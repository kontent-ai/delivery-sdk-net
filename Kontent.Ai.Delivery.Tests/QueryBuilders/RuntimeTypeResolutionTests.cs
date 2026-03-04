using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Generated;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kontent.Ai.Delivery.Tests.QueryBuilders;

/// <summary>
/// Tests for runtime type resolution in dynamic queries.
/// When a custom ITypeProvider is registered, dynamic queries should automatically
/// resolve items to their strongly-typed models at runtime.
/// </summary>
public class RuntimeTypeResolutionTests
{
    private readonly Guid _guid = Guid.NewGuid();
    private string BaseUrl => $"https://deliver.kontent.ai/{_guid}";

    /// <summary>
    /// Creates a client with the GeneratedTypeProvider registered (enables runtime type resolution).
    /// </summary>
    private IDeliveryClient CreateClientWithTypeProvider(MockHttpMessageHandler mockHttp)
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = _guid.ToString() },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        services.AddSingleton<ITypeProvider, GeneratedTypeProvider>();
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    /// <summary>
    /// Creates a client without a custom ITypeProvider (no runtime type resolution).
    /// </summary>
    private IDeliveryClient CreateClientWithoutTypeProvider(MockHttpMessageHandler mockHttp)
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = _guid.ToString() },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    #region GetItem - Single Item Tests

    [Fact]
    public async Task GetItem_WithTypeProvider_ResolvesToStronglyTypedArticle()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", await LoadFixtureAsync("coffee_beverages_explained.json"));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var result = await client.GetItem("coffee_beverages_explained").ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);

        // Should be runtime-typed to Article
        Assert.IsType<Kontent.Ai.Delivery.ContentItems.ContentItem<Article>>(result.Value);

        // Pattern matching should work
        if (result.Value is IContentItem<Article> article)
        {
            Assert.Equal("Coffee Beverages Explained", article.Elements.Title);
            Assert.Equal("coffee_beverages_explained", article.System.Codename);
            Assert.NotNull(article.Elements.BodyCopy);
            Assert.NotNull(article.Elements.TeaserImage);
        }
        else
        {
            Assert.Fail("Expected IContentItem<Article> but got different type");
        }
    }

    [Fact]
    public async Task GetItem_WithoutTypeProvider_ReturnsDynamicElements()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", await LoadFixtureAsync("coffee_beverages_explained.json"));

        var client = CreateClientWithoutTypeProvider(mockHttp);

        // Act
        var result = await client.GetItem("coffee_beverages_explained").ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);

        // Should remain as dynamic (no type provider registered)
        if (result.Value is IContentItem<IDynamicElements> dynamicItem)
        {
            Assert.Equal("coffee_beverages_explained", dynamicItem.System.Codename);

            // Can access elements via dictionary
            Assert.True(dynamicItem.Elements.TryGetValue("title", out var titleElement));
            Assert.Equal("Coffee Beverages Explained", titleElement.GetProperty("value").GetString());
        }
        else
        {
            Assert.Fail("Expected IContentItem<IDynamicElements> but got different type");
        }
    }

    [Fact]
    public async Task GetItem_WithTypeProvider_UnmappedType_ReturnsDynamicElements()
    {
        // Arrange - verify that when no type provider mapping exists, items fall back to dynamic
        // We use GetItems() and check the returned items for types not in GeneratedTypeProvider
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items")
            .Respond("application/json", await LoadFixtureAsync("items.json"));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var result = await client.GetItems().ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotEmpty(result.Value.Items);

        // Items with types in GeneratedTypeProvider should be runtime-typed
        var articleItems = result.Value.Items.Where(i => i.System.Type == "article").ToList();
        Assert.NotEmpty(articleItems);
        Assert.All(articleItems, item =>
        {
            Assert.True(item is IContentItem<Article>,
                $"Article item should be typed but was {item.GetType().Name}");
        });

        // Items with types NOT in GeneratedTypeProvider should remain dynamic
        // The items.json has "grinder" type which is not mapped
        var grinderItems = result.Value.Items.Where(i => i.System.Type == "grinder").ToList();
        if (grinderItems.Any())
        {
            Assert.All(grinderItems, item =>
            {
                Assert.IsAssignableFrom<IDynamicElements>(item.Elements);
            });
        }
    }

    #endregion

    #region GetItems - Multiple Items Tests

    [Fact]
    public async Task GetItems_WithTypeProvider_ResolvesEachItemToCorrectType()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items")
            .Respond("application/json", await LoadFixtureAsync("items.json"));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var result = await client.GetItems().ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Items);

        // Check that articles are typed correctly
        var articles = result.Value.Items
            .Where(i => i is IContentItem<Article>)
            .Cast<IContentItem<Article>>()
            .ToList();

        Assert.NotEmpty(articles);
        Assert.All(articles, article =>
        {
            Assert.Equal("article", article.System.Type);
            Assert.NotNull(article.Elements.Title);
        });
    }

    [Fact]
    public async Task GetItems_WithoutTypeProvider_AllItemsAreDynamic()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items")
            .Respond("application/json", await LoadFixtureAsync("items.json"));

        var client = CreateClientWithoutTypeProvider(mockHttp);

        // Act
        var result = await client.GetItems().ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Items);

        // All items should be dynamic
        Assert.All(result.Value.Items, item =>
        {
            Assert.True(item is IContentItem<IDynamicElements>,
                $"Expected IContentItem<IDynamicElements> but got {item.GetType().Name}");
        });
    }

    [Fact]
    public async Task GetItems_WithTypeProvider_FetchNextPage_PreservesRuntimeTyping()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var itemsUrl = $"{BaseUrl}/items";

        mockHttp.Expect(itemsUrl)
            .WithQueryString("limit", "1")
            .Respond("application/json",
                BuildPagedItemsListingJson(skip: 0, limit: 1, totalCount: 2, codename: "paged_article_1", hasNextPage: true));

        mockHttp.Expect(itemsUrl)
            .WithQueryString("skip", "1")
            .WithQueryString("limit", "1")
            .Respond("application/json",
                BuildPagedItemsListingJson(skip: 1, limit: 1, totalCount: 2, codename: "paged_article_2", hasNextPage: false));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var firstPageResult = await client.GetItems().Limit(1).ExecuteAsync();
        Assert.True(firstPageResult.IsSuccess);
        Assert.Single(firstPageResult.Value.Items);
        Assert.True(firstPageResult.Value.Items[0] is IContentItem<Article>);
        Assert.True(firstPageResult.Value.HasNextPage);

        var secondPageResult = await firstPageResult.Value.FetchNextPageAsync();

        // Assert
        Assert.NotNull(secondPageResult);
        Assert.True(secondPageResult.IsSuccess);
        Assert.Single(secondPageResult.Value.Items);
        Assert.True(secondPageResult.Value.Items[0] is IContentItem<Article>);
        Assert.False(secondPageResult.Value.HasNextPage);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion

    #region GetItemsFeed - Enumeration Tests

    [Fact]
    public async Task GetItemsFeed_WithTypeProvider_ResolvesItemsDuringEnumeration()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items-feed")
            .Respond("application/json", await LoadFixtureAsync("articles_feed.json"));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var items = new List<IContentItem>();
        await foreach (var item in client.GetItemsFeed().EnumerateItemsAsync())
        {
            items.Add(item);
        }

        // Assert
        Assert.NotEmpty(items);

        // Check articles are typed
        var articleItems = items.Where(i => i.System.Type == "article").ToList();
        Assert.All(articleItems, item =>
        {
            Assert.True(item is IContentItem<Article>,
                $"Expected IContentItem<Article> but got {item.GetType().Name}");
        });
    }

    [Fact]
    public async Task GetItemsFeed_ExecuteAsync_WithTypeProvider_ResolvesToCorrectTypes()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items-feed")
            .Respond("application/json", await LoadFixtureAsync("articles_feed.json"));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var result = await client.GetItemsFeed().ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Items);

        // Check that items are properly typed
        foreach (var item in result.Value.Items)
        {
            if (item.System.Type == "article")
            {
                Assert.True(item is IContentItem<Article>,
                    $"Item {item.System.Codename} should be IContentItem<Article>");
            }
        }
    }

    [Fact]
    public async Task GetItemsFeed_WithTypeProvider_FetchNextPage_PreservesRuntimeTyping()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var feedUrl = $"{BaseUrl}/items-feed";

        mockHttp.Expect(feedUrl)
            .Respond(_ => BuildFeedPageResponse(["feed_article_1"], continuationToken: "token_1"));

        mockHttp.Expect(feedUrl)
            .WithHeaders("X-Continuation", "token_1")
            .Respond(_ => BuildFeedPageResponse(["feed_article_2"], continuationToken: null));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var firstPageResult = await client.GetItemsFeed().ExecuteAsync();

        // Assert first page
        Assert.True(firstPageResult.IsSuccess);
        Assert.Single(firstPageResult.Value.Items);
        Assert.True(firstPageResult.Value.Items[0] is IContentItem<Article>);
        Assert.True(firstPageResult.Value.HasNextPage);

        var secondPageResult = await firstPageResult.Value.FetchNextPageAsync();

        // Assert second page
        Assert.NotNull(secondPageResult);
        Assert.True(secondPageResult.IsSuccess);
        Assert.Single(secondPageResult.Value.Items);
        Assert.True(secondPageResult.Value.Items[0] is IContentItem<Article>);
        Assert.False(secondPageResult.Value.HasNextPage);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion

    #region Linked Items and Hydration Tests

    [Fact]
    public async Task GetItems_WithTypeProvider_LinkedItemsAreStronglyTyped()
    {
        // Arrange - items.json has article_1 with related_articles: ["article_2"]
        // and article_2 in modular_content
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items")
            .Respond("application/json", await LoadFixtureAsync("items.json"));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act - dynamic GetItems()
        var result = await client.GetItems().ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);

        // Find article_1 which has related_articles -> article_2
        var article1 = result.Value.Items
            .Where(i => i.System.Codename == "article_1")
            .Cast<IContentItem<Article>>()
            .Single();

        Assert.NotNull(article1.Elements.RelatedArticles);
        var relatedArticles = article1.Elements.RelatedArticles!.ToList();
        Assert.Single(relatedArticles);

        // The linked item should be strongly typed, not DynamicElements
        var linkedArticle = relatedArticles[0];
        Assert.IsType<IEmbeddedContent<Article>>(linkedArticle, exactMatch: false);
        Assert.Equal("article_2", linkedArticle.System.Codename);
        Assert.Equal("article", linkedArticle.System.Type);

        // The linked item's elements should be hydrated
        var typedLinkedArticle = (IEmbeddedContent<Article>)linkedArticle;
        Assert.Equal("Title of article 2", typedLinkedArticle.Elements.Title);
    }

    [Fact]
    public async Task GetItems_StronglyTyped_LinkedItemsAreStronglyTyped()
    {
        // Arrange - same fixture but using strongly-typed GetItems<Article>()
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items")
            .Respond("application/json", await LoadFixtureAsync("items.json"));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act - strongly-typed listing
        var result = await client.GetItems<Article>().ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);

        var article1 = result.Value.Items
            .Single(i => i.System.Codename == "article_1");

        Assert.NotNull(article1.Elements.RelatedArticles);
        var relatedArticles = article1.Elements.RelatedArticles!.ToList();
        Assert.Single(relatedArticles);

        // The linked item should be strongly typed via the type provider
        var linkedArticle = relatedArticles[0];
        Assert.IsType<IEmbeddedContent<Article>>(linkedArticle, exactMatch: false);
        Assert.Equal("article_2", linkedArticle.System.Codename);

        var typedLinkedArticle = (IEmbeddedContent<Article>)linkedArticle;
        Assert.Equal("Title of article 2", typedLinkedArticle.Elements.Title);
    }

    [Fact]
    public async Task GetItems_WithoutTypeProvider_LinkedItemsFallBackToDynamic()
    {
        // Arrange - without type provider, linked items should be DynamicElements
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items")
            .Respond("application/json", await LoadFixtureAsync("items.json"));

        var client = CreateClientWithoutTypeProvider(mockHttp);

        // Act
        var result = await client.GetItems().ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);

        // All items should be dynamic since there's no type provider
        var article1 = result.Value.Items
            .Single(i => i.System.Codename == "article_1");

        Assert.IsAssignableFrom<IContentItem<IDynamicElements>>(article1);
    }

    [Fact]
    public async Task GetItem_WithTypeProvider_LinkedItemsAreStronglyTyped()
    {
        // Arrange - single item dynamic query, verify linked items are also typed
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items/on_roasts")
            .Respond("application/json", await LoadFixtureAsync("on_roasts.json"));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act - dynamic single item
        var result = await client.GetItem("on_roasts").ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value is IContentItem<Article>,
            $"Expected IContentItem<Article> but got {result.Value.GetType().Name}");

        var article = (IContentItem<Article>)result.Value;
        Assert.NotNull(article.Elements.RelatedArticles);

        var relatedArticles = article.Elements.RelatedArticles!.ToList();
        Assert.Equal(2, relatedArticles.Count);

        // Each linked item should be strongly typed, not DynamicElements
        foreach (var linkedItem in relatedArticles)
        {
            Assert.IsType<IEmbeddedContent<Article>>(linkedItem, exactMatch: false);
            Assert.Equal("article", linkedItem.System.Type);
        }

        // Verify specific linked items are fully hydrated
        var processingTechniques = relatedArticles
            .OfType<IEmbeddedContent<Article>>()
            .Single(a => a.System.Codename == "coffee_processing_techniques");
        Assert.Equal("Coffee processing techniques", processingTechniques.Elements.Title);

        var arabicaBourbon = relatedArticles
            .OfType<IEmbeddedContent<Article>>()
            .Single(a => a.System.Codename == "origins_of_arabica_bourbon");
        Assert.Equal("Origins of Arabica Bourbon", arabicaBourbon.Elements.Title);
    }

    [Fact]
    public async Task GetItemsFeed_WithTypeProvider_LinkedItemsAreStronglyTyped()
    {
        // Arrange - build a feed response with linked items
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items-feed")
            .Respond("application/json", BuildFeedResponseWithLinkedItems());

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var items = new List<IContentItem>();
        await foreach (var item in client.GetItemsFeed().EnumerateItemsAsync())
        {
            items.Add(item);
        }

        // Assert
        Assert.Single(items);
        Assert.True(items[0] is IContentItem<Article>);

        var article = (IContentItem<Article>)items[0];
        Assert.NotNull(article.Elements.RelatedArticles);

        var relatedArticles = article.Elements.RelatedArticles!.ToList();
        Assert.Single(relatedArticles);

        // Linked item should be strongly typed
        var linkedItem = relatedArticles[0];
        Assert.IsType<IEmbeddedContent<Article>>(linkedItem, exactMatch: false);
        Assert.Equal("linked_article", linkedItem.System.Codename);

        var typedLinked = (IEmbeddedContent<Article>)linkedItem;
        Assert.Equal("Linked Article Title", typedLinked.Elements.Title);
    }

    [Fact]
    public async Task GetItem_WithTypeProvider_HydratesComplexElements()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", await LoadFixtureAsync("coffee_beverages_explained.json"));

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var result = await client.GetItem("coffee_beverages_explained").ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);

        if (result.Value is IContentItem<Article> article)
        {
            // Rich text should be hydrated
            Assert.NotNull(article.Elements.BodyCopy);
            Assert.NotEmpty(article.Elements.BodyCopy);

            // Assets should be hydrated
            Assert.NotNull(article.Elements.TeaserImage);
            var asset = article.Elements.TeaserImage.FirstOrDefault();
            Assert.NotNull(asset);
            Assert.NotNull(asset.Url);
            Assert.NotNull(asset.Name);

            // Taxonomy should be hydrated
            Assert.NotNull(article.Elements.Personas);
            var persona = article.Elements.Personas.FirstOrDefault();
            Assert.NotNull(persona);
            Assert.NotNull(persona.Codename);

            // DateTime should be hydrated
            Assert.NotNull(article.Elements.PostDate);
            Assert.NotNull(article.Elements.PostDate.Value);
        }
        else
        {
            Assert.Fail("Expected IContentItem<Article>");
        }
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public async Task PatternMatching_SwitchExpression_WorksWithRuntimeTypedItems()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", await LoadFixtureAsync("coffee_beverages_explained.json"));

        var client = CreateClientWithTypeProvider(mockHttp);
        var result = await client.GetItem("coffee_beverages_explained").ExecuteAsync();
        Assert.True(result.IsSuccess);

        // Act - Use switch expression pattern matching
        var title = result.Value switch
        {
            IContentItem<Article> article => article.Elements.Title,
            IContentItem<Coffee> coffee => coffee.Elements.ProductName,
            IContentItem<IDynamicElements> dynamic => dynamic.Elements["title"].GetProperty("value").GetString(),
            _ => null
        };

        // Assert
        Assert.Equal("Coffee Beverages Explained", title);
    }

    [Fact]
    public async Task PatternMatching_IsOperator_WorksWithRuntimeTypedItems()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", await LoadFixtureAsync("coffee_beverages_explained.json"));

        var client = CreateClientWithTypeProvider(mockHttp);
        var result = await client.GetItem("coffee_beverages_explained").ExecuteAsync();

        // Assert using 'is' pattern
        Assert.True(result.Value is IContentItem<Article>);
        Assert.False(result.Value is IContentItem<Coffee>);
        Assert.False(result.Value is IContentItem<IDynamicElements>);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetItem_ApiError_ReturnsFailureResult()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items/nonexistent")
            .Respond(HttpStatusCode.NotFound, "application/json", """{"message": "Not found", "request_id": "123"}""");

        var client = CreateClientWithTypeProvider(mockHttp);

        // Act
        var result = await client.GetItem("nonexistent").ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetItems_WithAlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/items")
            .Respond("application/json", await LoadFixtureAsync("items.json"));

        var client = CreateClientWithTypeProvider(mockHttp);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Already cancelled

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetItems().ExecuteAsync(cts.Token));
    }

    #endregion

    private static async Task<string> LoadFixtureAsync(string filename)
    {
        var path = Path.Combine(
            Environment.CurrentDirectory,
            $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{filename}");
        return await File.ReadAllTextAsync(path);
    }

    private static string BuildPagedItemsListingJson(int skip, int limit, int totalCount, string codename, bool hasNextPage)
    {
        var nextPageUrl = hasNextPage
            ? $"https://deliver.kontent.ai/items?skip={skip + limit}&limit={limit}"
            : string.Empty;

        return $$"""
            {
              "items": [
                {
                  "system": {
                    "id": "{{Guid.NewGuid()}}",
                    "name": "{{codename}}",
                    "codename": "{{codename}}",
                    "language": "en-US",
                    "type": "article",
                    "collection": "default",
                    "last_modified": "2024-01-01T00:00:00Z"
                  },
                  "elements": {
                    "title": {
                      "type": "text",
                      "name": "Title",
                      "value": "Title {{codename}}"
                    }
                  }
                }
              ],
              "pagination": {
                "skip": {{skip}},
                "limit": {{limit}},
                "count": 1,
                "total_count": {{totalCount}},
                "next_page": "{{nextPageUrl}}"
              },
              "modular_content": {}
            }
            """;
    }

    private static HttpResponseMessage BuildFeedPageResponse(IReadOnlyList<string> codenames, string? continuationToken)
    {
        var itemsJson = string.Join(",", codenames.Select(codename => $$"""
            {
              "system": {
                "id": "{{Guid.NewGuid()}}",
                "name": "{{codename}}",
                "codename": "{{codename}}",
                "language": "en-US",
                "type": "article",
                "collection": "default",
                "last_modified": "2024-01-01T00:00:00Z"
              },
              "elements": {
                "title": {
                  "type": "text",
                  "name": "Title",
                  "value": "Title {{codename}}"
                }
              }
            }
            """));

        var json = $$"""
            {
              "items": [{{itemsJson}}],
              "modular_content": {}
            }
            """;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(continuationToken))
            response.Headers.Add("X-Continuation", continuationToken);

        return response;
    }

    private static string BuildFeedResponseWithLinkedItems()
    {
        return $$"""
            {
              "items": [
                {
                  "system": {
                    "id": "{{Guid.NewGuid()}}",
                    "name": "Parent Article",
                    "codename": "parent_article",
                    "language": "en-US",
                    "type": "article",
                    "collection": "default",
                    "last_modified": "2024-01-01T00:00:00Z"
                  },
                  "elements": {
                    "title": {
                      "type": "text",
                      "name": "Title",
                      "value": "Parent Article Title"
                    },
                    "related_articles": {
                      "type": "modular_content",
                      "name": "Related articles",
                      "value": ["linked_article"]
                    }
                  }
                }
              ],
              "modular_content": {
                "linked_article": {
                  "system": {
                    "id": "{{Guid.NewGuid()}}",
                    "name": "Linked Article",
                    "codename": "linked_article",
                    "language": "en-US",
                    "type": "article",
                    "collection": "default",
                    "last_modified": "2024-01-01T00:00:00Z"
                  },
                  "elements": {
                    "title": {
                      "type": "text",
                      "name": "Title",
                      "value": "Linked Article Title"
                    },
                    "related_articles": {
                      "type": "modular_content",
                      "name": "Related articles",
                      "value": []
                    }
                  }
                }
              }
            }
            """;
    }
}
