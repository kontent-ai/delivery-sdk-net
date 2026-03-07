using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Mapping;

public sealed class ElementValueMapperTests
{
    [Fact]
    public async Task ElementValueMapper_MapSimpleValue_DeserializationFailure_SkipsValue()
    {
        var logger = new CollectingLogger<ElementValueMapper>();
        var mapper = CreateMapper(logger);
        var property = Assert.Single(PropertyMappingInfo.CreateMappings(typeof(SimpleModel)));

        using var doc = JsonDocument.Parse(
            """
            {
              "value": "not-an-int"
            }
            """);

        var context = new MappingContext
        {
            CancellationToken = CancellationToken.None
        };

        var value = await mapper.MapElementAsync(
            property,
            doc.RootElement,
            _ => Task.FromResult<object?>(null),
            context);

        Assert.Null(value);
        Assert.Contains(
            logger.Entries,
            entry => entry.EventId == LogEventIds.PropertyDeserializationFailed &&
                     entry.Message.Contains("deserialization failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ElementValueMapper_MapSimpleValue_MissingValueProperty_LogsMappingSkipped()
    {
        var logger = new CollectingLogger<ElementValueMapper>();
        var mapper = CreateMapper(logger);
        var property = Assert.Single(PropertyMappingInfo.CreateMappings(typeof(SimpleModel)));

        using var doc = JsonDocument.Parse(
            """
            {
              "type": "number"
            }
            """);

        var context = new MappingContext
        {
            CancellationToken = CancellationToken.None
        };

        var value = await mapper.MapElementAsync(
            property,
            doc.RootElement,
            _ => Task.FromResult<object?>(null),
            context);

        Assert.Null(value);
        Assert.Contains(
            logger.Entries,
            entry => entry.EventId == LogEventIds.ElementMappingSkipped &&
                     entry.Message.Contains("mapping skipped", StringComparison.OrdinalIgnoreCase));
    }

    private static ElementValueMapper CreateMapper(ILogger<ElementValueMapper>? logger = null)
    {
        var jsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();
        return new ElementValueMapper(
            NullContentDependencyExtractor.Instance,
            jsonOptions,
            new HtmlParser(),
            logger);
    }

    private sealed class SimpleModel
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    private sealed record LogEntry(int EventId, string Message);

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries => _entries;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => _entries.Add(new LogEntry(eventId.Id, formatter(state, exception)));
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    [Fact]
    public async Task MapAssets_WithCustomAssetDomain_RewritesAssetUrls()
    {
        var mapper = CreateMapper();
        var assetProperty = PropertyMappingInfo.CreateMappings(typeof(AssetModel))
            .Single(p => p.ElementCodename == "teaser_image");

        using var doc = JsonDocument.Parse(
            """
            {
              "type": "asset",
              "name": "Teaser image",
              "value": [
                {
                  "name": "hero.jpg",
                  "description": "Hero image",
                  "type": "image/jpeg",
                  "size": 12345,
                  "url": "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/hero.jpg",
                  "width": 800,
                  "height": 600,
                  "renditions": {}
                }
              ]
            }
            """);

        var context = new MappingContext
        {
            CustomAssetDomain = new Uri("https://assets.example.com"),
            CancellationToken = CancellationToken.None
        };

        var value = await mapper.MapElementAsync(
            assetProperty,
            doc.RootElement,
            _ => Task.FromResult<object?>(null),
            context);

        var assets = Assert.IsType<List<Asset>>(value);
        var asset = Assert.Single(assets);
        Assert.Equal("https://assets.example.com/env-id/asset-id/hero.jpg", asset.Url);
    }

    [Fact]
    public async Task MapAssets_WithCustomAssetDomainAndRenditionPreset_AppliesBoth()
    {
        var mapper = CreateMapper();
        var assetProperty = PropertyMappingInfo.CreateMappings(typeof(AssetModel))
            .Single(p => p.ElementCodename == "teaser_image");

        using var doc = JsonDocument.Parse(
            """
            {
              "type": "asset",
              "name": "Teaser image",
              "value": [
                {
                  "name": "hero.jpg",
                  "description": "Hero image",
                  "type": "image/jpeg",
                  "size": 12345,
                  "url": "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/hero.jpg",
                  "width": 800,
                  "height": 600,
                  "renditions": {
                    "mobile": {
                      "rendition_id": "r1",
                      "preset_id": "p1",
                      "width": 200,
                      "height": 150,
                      "query": "w=200&h=150"
                    }
                  }
                }
              ]
            }
            """);

        var context = new MappingContext
        {
            DefaultRenditionPreset = "mobile",
            CustomAssetDomain = new Uri("https://assets.example.com"),
            CancellationToken = CancellationToken.None
        };

        var value = await mapper.MapElementAsync(
            assetProperty,
            doc.RootElement,
            _ => Task.FromResult<object?>(null),
            context);

        var assets = Assert.IsType<List<Asset>>(value);
        var asset = Assert.Single(assets);
        Assert.Equal("https://assets.example.com/env-id/asset-id/hero.jpg?w=200&h=150", asset.Url);
    }

    [Fact]
    public async Task MapRichText_WithCustomAssetDomain_RewritesInlineImageUrls()
    {
        var mapper = CreateMapper();
        var richTextProperty = PropertyMappingInfo.CreateMappings(typeof(RichTextModel))
            .Single(p => p.ElementCodename == "body_copy");

        using var doc = JsonDocument.Parse(
            """
            {
              "type": "rich_text",
              "name": "Body copy",
              "value": "<p>Hello</p><figure data-asset-id=\"11111111-1111-1111-1111-111111111111\" data-image-id=\"11111111-1111-1111-1111-111111111111\"><img src=\"https://assets-eu-01.kc-usercontent.com/env-id/asset-id/image.jpg\" data-asset-id=\"11111111-1111-1111-1111-111111111111\" /></figure>",
              "images": {
                "11111111-1111-1111-1111-111111111111": {
                  "image_id": "11111111-1111-1111-1111-111111111111",
                  "description": "Test image",
                  "url": "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/image.jpg",
                  "height": 400,
                  "width": 600
                }
              },
              "links": {},
              "modular_content": []
            }
            """);

        var context = new MappingContext
        {
            CustomAssetDomain = new Uri("https://assets.example.com"),
            CancellationToken = CancellationToken.None
        };

        var value = await mapper.MapElementAsync(
            richTextProperty,
            doc.RootElement,
            _ => Task.FromResult<object?>(null),
            context);

        var richText = Assert.IsAssignableFrom<IRichTextContent>(value);
        var imageBlock = richText.OfType<IInlineImage>().Single();
        Assert.Equal("https://assets.example.com/env-id/asset-id/image.jpg", imageBlock.Url);
    }

    private sealed class AssetModel
    {
        [JsonPropertyName("teaser_image")]
        public IEnumerable<IAsset>? TeaserImage { get; set; }
    }

    private sealed class RichTextModel
    {
        [JsonPropertyName("body_copy")]
        public IRichTextContent? BodyCopy { get; set; }
    }
}
