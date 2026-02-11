using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

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
        var optionsMonitor = new StaticOptionsMonitor<DeliveryOptions>(new DeliveryOptions());
        return new ElementValueMapper(
            optionsMonitor,
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

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
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
}
