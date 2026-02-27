using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems;

public class DeliveryItemResponseTests
{
    private static readonly JsonSerializerOptions Options = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();

    [Fact]
    public void ExplicitInterface_Item_ReturnsSameInstance()
    {
        var item = DeserializeItem();
        var modularContent = new Dictionary<string, JsonElement>();

        var response = new DeliveryItemResponse<IDynamicElements>
        {
            Item = item,
            ModularContent = modularContent
        };

        IDeliveryItemResponse<IDynamicElements> iface = response;

        Assert.Same(item, iface.Item);
    }

    [Fact]
    public void WithExpression_CreatesClone()
    {
        var item = DeserializeItem();
        var modularContent = new Dictionary<string, JsonElement>();

        var response = new DeliveryItemResponse<IDynamicElements>
        {
            Item = item,
            ModularContent = modularContent
        };

        var clone = response with { };

        Assert.NotSame(response, clone);
        Assert.Same(response.Item, clone.Item);
        Assert.Same(response.ModularContent, clone.ModularContent);
    }

    private static ContentItem<IDynamicElements> DeserializeItem()
        => JsonSerializer.Deserialize<ContentItem<IDynamicElements>>("""
        {
            "system": {
                "id": "00000000-0000-0000-0000-000000000001",
                "name": "Test",
                "codename": "test",
                "type": "article",
                "collection": "default",
                "workflow": "default",
                "workflow_step": "published",
                "language": "en-US",
                "last_modified": "2024-01-01T00:00:00Z",
                "sitemap_locations": []
            },
            "elements": {}
        }
        """, Options)!;
}
