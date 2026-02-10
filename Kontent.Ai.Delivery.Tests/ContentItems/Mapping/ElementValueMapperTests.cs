using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Mapping;

public sealed class ElementValueMapperTests
{
    [Fact]
    public async Task ElementValueMapper_MapSimpleValue_DeserializationFailure_SkipsValue()
    {
        var mapper = CreateMapper();
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
    }

    private static ElementValueMapper CreateMapper()
    {
        var jsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();
        var optionsMonitor = new StaticOptionsMonitor<DeliveryOptions>(new DeliveryOptions());
        return new ElementValueMapper(
            optionsMonitor,
            NullContentDependencyExtractor.Instance,
            jsonOptions,
            new HtmlParser());
    }

    private sealed class SimpleModel
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
