using System.Text.Json;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Caching;

/// <summary>
/// Unit tests for cache payload creation on <see cref="CachedRawItemsPayload"/>.
/// </summary>
public class CachePayloadTests
{
    [Fact]
    public void CachedRawItemsPayload_FromItem_WithValidItem_ExtractsRawJson()
    {
        // Arrange
        var rawJson = """{"system":{"codename":"test-item","type":"article"},"elements":{"title":{"type":"text","value":"Test Title"}}}""";
        using var doc = JsonDocument.Parse(rawJson);
        var item = CreateContentItem("test-item", doc.RootElement.Clone());

        var modularContent = new Dictionary<string, JsonElement>
        {
            ["linked-item"] = JsonDocument.Parse("""{"system":{"codename":"linked-item"}}""").RootElement
        };

        // Act
        var payload = CachedRawItemsPayload.FromItem(item, modularContent);

        // Assert
        Assert.NotNull(payload);
        Assert.Single(payload.ItemsJson);
        Assert.Contains("test-item", payload.ItemsJson[0]);
        Assert.Single(payload.ModularContentJson);
        Assert.True(payload.ModularContentJson.ContainsKey("linked-item"));
        Assert.Null(payload.Pagination);
    }

    [Fact]
    public void CachedRawItemsPayload_FromItem_WithNullModularContent_ReturnsEmptyDictionary()
    {
        // Arrange
        var rawJson = """{"system":{"codename":"test-item"},"elements":{}}""";
        using var doc = JsonDocument.Parse(rawJson);
        var item = CreateContentItem("test-item", doc.RootElement.Clone());

        // Act
        var payload = CachedRawItemsPayload.FromItem<Article>(item, null);

        // Assert
        Assert.NotNull(payload);
        Assert.Empty(payload.ModularContentJson);
    }

    [Fact]
    public void CachedRawItemsPayload_FromItem_WithoutRawJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var item = CreateContentItem("test-item", rawJson: null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => CachedRawItemsPayload.FromItem<Article>(item, null));

        Assert.Contains("test-item", exception.Message);
    }

    [Fact]
    public void CachedRawItemsPayload_FromListing_WithValidItems_ExtractsAllRawJson()
    {
        // Arrange
        var rawJson1 = """{"system":{"codename":"item-1"},"elements":{}}""";
        var rawJson2 = """{"system":{"codename":"item-2"},"elements":{}}""";

        using var doc1 = JsonDocument.Parse(rawJson1);
        using var doc2 = JsonDocument.Parse(rawJson2);

        var items = new List<ContentItem<Article>>
        {
            CreateContentItem("item-1", doc1.RootElement.Clone()),
            CreateContentItem("item-2", doc2.RootElement.Clone())
        };

        var response = new DeliveryItemListingResponse<Article>
        {
            Items = items,
            Pagination = new Pagination { Skip = 0, Limit = 10, Count = 2, NextPageUrl = "" },
            ModularContent = new Dictionary<string, JsonElement>()
        };

        // Act
        var payload = CachedRawItemsPayload.FromListing(response);

        // Assert
        Assert.NotNull(payload);
        Assert.Equal(2, payload.ItemsJson.Count);
        Assert.Contains("item-1", payload.ItemsJson[0]);
        Assert.Contains("item-2", payload.ItemsJson[1]);
        var pagination = Assert.IsType<Pagination>(payload.Pagination);
        Assert.Equal(0, pagination.Skip);
        Assert.Equal(10, pagination.Limit);
    }

    [Fact]
    public void CachedRawItemsPayload_FromListing_WithModularContent_ExtractsModularContentJson()
    {
        // Arrange
        var rawJson = """{"system":{"codename":"item-1"},"elements":{}}""";
        using var doc = JsonDocument.Parse(rawJson);

        var items = new List<ContentItem<Article>>
        {
            CreateContentItem("item-1", doc.RootElement.Clone())
        };

        var linkedItemJson = JsonDocument.Parse("""{"system":{"codename":"linked"}}""").RootElement;
        var response = new DeliveryItemListingResponse<Article>
        {
            Items = items,
            Pagination = new Pagination { Skip = 0, Limit = 10, Count = 1, NextPageUrl = "" },
            ModularContent = new Dictionary<string, JsonElement>
            {
                ["linked"] = linkedItemJson
            }
        };

        // Act
        var payload = CachedRawItemsPayload.FromListing(response);

        // Assert
        Assert.Single(payload.ModularContentJson);
        Assert.True(payload.ModularContentJson.ContainsKey("linked"));
        Assert.Contains("linked", payload.ModularContentJson["linked"]);
    }

    [Fact]
    public void CachedRawItemsPayload_FromListing_WithEmptyItems_ReturnsEmptyList()
    {
        // Arrange
        var response = new DeliveryItemListingResponse<Article>
        {
            Items = [],
            Pagination = new Pagination { Skip = 0, Limit = 10, Count = 0, NextPageUrl = "" },
            ModularContent = new Dictionary<string, JsonElement>()
        };

        // Act
        var payload = CachedRawItemsPayload.FromListing(response);

        // Assert
        Assert.Empty(payload.ItemsJson);
    }

    private static ContentItem<Article> CreateContentItem(string codename, JsonElement? rawJson)
    {
        return new ContentItem<Article>
        {
            System = new ContentItemSystemAttributes
            {
                Id = Guid.NewGuid(),
                Name = codename,
                Codename = codename,
                Type = "article",
                Language = "en-US",
                LastModified = DateTime.UtcNow,
                Collection = "default"
            },
            Elements = new Article(),
            RawItemJson = rawJson
        };
    }
}
