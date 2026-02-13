using System.Collections.Concurrent;
using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Logging;

public class DynamicQueryLoggingTests
{
    [Fact]
    public async Task DynamicGetItem_Success_EmitsStartingAndCompletedLogs()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemCodename = "coffee_beverages_explained";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/items/{itemCodename}")
            .Respond("application/json", await LoadFixtureAsync($"{itemCodename}.json"));

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetItem(itemCodename).ExecuteAsync();

        Assert.True(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting Item query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryCompleted, "Completed Item query");
    }

    [Fact]
    public async Task DynamicGetItem_Failure_EmitsFailedLog()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemCodename = "missing_item";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/items/{itemCodename}")
            .Respond(HttpStatusCode.NotFound, "application/json", """{"message":"Not found","request_id":"req"}""");

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetItem(itemCodename).ExecuteAsync();

        Assert.False(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting Item query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryFailed, "Query Item failed");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryCompleted, "Completed Item query");
    }

    [Fact]
    public async Task DynamicGetItems_Success_EmitsStartingAndCompletedLogs()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/items")
            .Respond("application/json", await LoadFixtureAsync("items.json"));

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetItems().ExecuteAsync();

        Assert.True(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting Items query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryCompleted, "Completed Items query");
    }

    [Fact]
    public async Task DynamicGetItems_Failure_EmitsFailedLog()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/items")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", "Server error");

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetItems().ExecuteAsync();

        Assert.False(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting Items query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryFailed, "Query Items failed");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryCompleted, "Completed Items query");
    }

    [Fact]
    public async Task GetType_Failure_EmitsFailedLog()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/types/missing_type")
            .Respond(HttpStatusCode.NotFound, "application/json", """{"message":"Not found","request_id":"req"}""");

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetType("missing_type").ExecuteAsync();

        Assert.False(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting Type query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryFailed, "Query Type failed");
    }

    [Fact]
    public async Task GetTypes_Failure_EmitsFailedLog()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/types")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", "Server error");

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetTypes().ExecuteAsync();

        Assert.False(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting Types query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryFailed, "Query Types failed");
    }

    [Fact]
    public async Task GetTaxonomy_Failure_EmitsFailedLog()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/taxonomies/missing_taxonomy")
            .Respond(HttpStatusCode.NotFound, "application/json", """{"message":"Not found","request_id":"req"}""");

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetTaxonomy("missing_taxonomy").ExecuteAsync();

        Assert.False(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting Taxonomy query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryFailed, "Query Taxonomy failed");
    }

    [Fact]
    public async Task GetTaxonomies_Failure_EmitsFailedLog()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/taxonomies")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", "Server error");

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetTaxonomies().ExecuteAsync();

        Assert.False(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting Taxonomies query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryFailed, "Query Taxonomies failed");
    }

    [Fact]
    public async Task GetLanguages_Success_EmitsStartingAndCompletedLogs()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/languages")
            .Respond("application/json", await LoadFixtureAsync("languages.json"));

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetLanguages().ExecuteAsync();

        Assert.True(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting Languages query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryCompleted, "Completed Languages query");
    }

    [Fact]
    public async Task GetContentElement_Success_EmitsStartingAndCompletedLogs()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/types/article/elements/title")
            .Respond("application/json", "{\"type\":\"text\",\"name\":\"Title\",\"codename\":\"title\"}");

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetContentElement("article", "title").ExecuteAsync();

        Assert.True(result.IsSuccess);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStarting, "Starting TypeElement query");
        AssertLog(loggerProvider.Entries, LogEventIds.QueryCompleted, "Completed TypeElement query");
    }

    [Fact]
    public async Task GetType_StaleContent_EmitsStaleContentLog()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var headers = new[] { new KeyValuePair<string, string>("X-Stale-Content", "1") };
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/types/article")
            .Respond(headers, "application/json", await LoadFixtureAsync("article.json"));

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetType("article").ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.HasStaleContent);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStaleContent, "stale content");
    }

    [Fact]
    public async Task GetTypes_StaleContent_EmitsStaleContentLog()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var headers = new[] { new KeyValuePair<string, string>("X-Stale-Content", "1") };
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{baseUrl}/types")
            .Respond(headers, "application/json", await LoadFixtureAsync("types_accessory.json"));

        var loggerProvider = new CollectingLoggerProvider();
        var client = CreateClient(env, mockHttp, loggerProvider);

        var result = await client.GetTypes().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.HasStaleContent);
        AssertLog(loggerProvider.Entries, LogEventIds.QueryStaleContent, "stale content");
    }

    private static IDeliveryClient CreateClient(string env, MockHttpMessageHandler mockHttp, CollectingLoggerProvider loggerProvider)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(loggerProvider);
        });

        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = env },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    private static async Task<string> LoadFixtureAsync(string filename)
    {
        var path = Path.Combine(
            Environment.CurrentDirectory,
            $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{filename}");
        return await File.ReadAllTextAsync(path);
    }

    private static void AssertLog(IEnumerable<LogEntry> entries, int eventId, string messageFragment) => Assert.Contains(entries, e => e.EventId == eventId && e.Message.Contains(messageFragment, StringComparison.Ordinal));

    private sealed record LogEntry(int EventId, LogLevel Level, string Message, string CategoryName);

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogEntry> _entries = new();

        public IReadOnlyCollection<LogEntry> Entries => _entries.ToArray();

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(categoryName, _entries);

        public void Dispose()
        {
        }
    }

    private sealed class CollectingLogger(string categoryName, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new LogEntry(
                eventId.Id,
                logLevel,
                formatter(state, exception),
                categoryName));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
