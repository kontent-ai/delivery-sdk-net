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
