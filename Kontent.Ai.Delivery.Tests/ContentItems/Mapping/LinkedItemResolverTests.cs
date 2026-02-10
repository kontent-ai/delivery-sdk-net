using System.Text.Json;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Mapping;

public sealed class LinkedItemResolverTests
{
    [Fact]
    public async Task LinkedItemResolver_CircularReference_ReturnsSameInstance()
    {
        var resolver = CreateResolver();
        var inProgressInstance = new object();

        var context = new MappingContext
        {
            ModularContent = new Dictionary<string, JsonElement>
            {
                ["on_roasts"] = ParseJsonElement(
                    """
                    {
                      "system": {
                        "type": "article"
                      }
                    }
                    """)
            },
            CancellationToken = CancellationToken.None
        };
        context.ItemsBeingHydrated["on_roasts"] = inProgressInstance;

        var result = await resolver.ResolveAsync(
            "on_roasts",
            context,
            static (_, _, _) => Task.CompletedTask);

        Assert.Same(inProgressInstance, result);
    }

    [Fact]
    public async Task LinkedItemResolver_MissingModularContent_ReturnsNullWithoutThrow()
    {
        var resolver = CreateResolver();
        var context = new MappingContext
        {
            ModularContent = null,
            CancellationToken = CancellationToken.None
        };

        var result = await resolver.ResolveAsync(
            "missing_item",
            context,
            static (_, _, _) => Task.CompletedTask);

        Assert.Null(result);
    }

    private static LinkedItemResolver CreateResolver()
    {
        var jsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();
        var typingStrategy = new DefaultItemTypingStrategy(new TypeProvider());
        var deserializer = new ContentDeserializer(jsonOptions);
        return new LinkedItemResolver(typingStrategy, deserializer);
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
