using System.Net;
using System.Text;
using Kontent.Ai.Delivery;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryBuilders;

public sealed class UsedInQueriesTests
{
    [Fact]
    public async Task UsedInQueries_Continuation_PaginatesAcrossMultiplePages()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/items/coffee_beverages_explained/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .Respond(_ => CreateUsedInResponse(["parent_1", "parent_2"], continuationToken: "token_1"));

        mockHttp.Expect(usedInUrl)
            .WithHeaders("X-Continuation", "token_1")
            .Respond(_ => CreateUsedInResponse(["parent_3"], continuationToken: null));

        var client = BuildClient(env, mockHttp, new DeliveryOptions
        {
            EnvironmentId = env,
            EnableResilience = false
        });
        var items = new List<IUsedInItem>();

        await foreach (var item in client.GetItemUsedIn("coffee_beverages_explained").EnumerateItemsAsync())
            items.Add(item);

        Assert.Equal(["parent_1", "parent_2", "parent_3"], items.Select(x => x.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UsedInQueries_FailedIntermediatePage_StopsEnumerationWithoutThrow()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/assets/asset_codename/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .Respond(_ => CreateUsedInResponse(["parent_1"], continuationToken: "token_1"));

        mockHttp.Expect(usedInUrl)
            .Respond(HttpStatusCode.ServiceUnavailable);

        var client = BuildClient(env, mockHttp, new DeliveryOptions
        {
            EnvironmentId = env,
            EnableResilience = false
        });
        var items = new List<IUsedInItem>();

        await foreach (var item in client.GetAssetUsedIn("asset_codename").EnumerateItemsAsync())
            items.Add(item);

        Assert.Single(items);
        Assert.Equal("parent_1", items[0].System.Codename);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UsedInQueries_FailedIntermediatePage_WithStatusEnumeration_YieldsFailureResult()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/assets/asset_codename/used-in";
        var mockHttp = new MockHttpMessageHandler();
        var callCount = 0;
        mockHttp.When(usedInUrl)
            .Respond(_ =>
            {
                callCount++;
                return callCount == 1
                    ? CreateUsedInResponse(["parent_1"], continuationToken: "token_1")
                    : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });

        var client = BuildClient(env, mockHttp);
        var pageResults = new List<IDeliveryResult<IReadOnlyList<IUsedInItem>>>();

        await foreach (var page in client.GetAssetUsedIn("asset_codename").EnumerateItemsWithStatusAsync())
        {
            pageResults.Add(page);
        }

        Assert.Equal(2, pageResults.Count);
        Assert.True(pageResults[0].IsSuccess);
        Assert.Single(pageResults[0].Value);
        Assert.Equal("parent_1", pageResults[0].Value[0].System.Codename);
        Assert.False(pageResults[1].IsSuccess);
    }

    [Fact]
    public async Task UsedInQueries_QueryWaitEnabled_AddsHeader()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/items/coffee_beverages_explained/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .With(req => req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond(_ => CreateUsedInResponse(["parent_1"], continuationToken: null));

        var client = BuildClient(env, mockHttp, new DeliveryOptions { EnvironmentId = env });
        var items = new List<IUsedInItem>();

        await foreach (var item in client.GetItemUsedIn("coffee_beverages_explained")
                           .WaitForLoadingNewContent()
                           .EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.Single(items);
        Assert.Equal("parent_1", items[0].System.Codename);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UsedInQueries_QueryWaitFalse_OmitsHeader()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/items/coffee_beverages_explained/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .With(req => !req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond(_ => CreateUsedInResponse(["parent_1"], continuationToken: null));

        var client = BuildClient(env, mockHttp, new DeliveryOptions { EnvironmentId = env });

        var items = new List<IUsedInItem>();
        await foreach (var item in client.GetItemUsedIn("coffee_beverages_explained")
                           .WaitForLoadingNewContent(false)
                           .EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.Single(items);
        Assert.Equal("parent_1", items[0].System.Codename);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UsedInQueries_FailedIntermediatePage_EmitsPaginationStoppedEarlyWarning()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/assets/asset_codename/used-in";
        var mockHttp = new MockHttpMessageHandler();
        var callCount = 0;
        mockHttp.When(usedInUrl)
            .Respond(_ =>
            {
                callCount++;
                return callCount == 1
                    ? CreateUsedInResponse(["parent_1"], continuationToken: "token_1")
                    : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });

        var loggerProvider = new CollectingLoggerProvider();
        var client = BuildClient(env, mockHttp, new DeliveryOptions
        {
            EnvironmentId = env,
            EnableResilience = false
        }, loggerProvider);

        await foreach (var _ in client.GetAssetUsedIn("asset_codename").EnumerateItemsAsync())
        {
            // Intentionally empty - verify log side effect.
        }

        Assert.Contains(
            loggerProvider.Entries,
            entry => entry.EventId == Kontent.Ai.Delivery.Logging.LogEventIds.PaginationStoppedEarly &&
                     entry.Message.Contains("AssetUsedIn", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ItemUsedIn_Where_SendsFilterInQueryString()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/items/my_item/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .WithQueryString("system.type[eq]", "article")
            .Respond(_ => CreateUsedInResponse(["parent_1"], continuationToken: null));

        var client = BuildClient(env, mockHttp, new DeliveryOptions
        {
            EnvironmentId = env,
            EnableResilience = false
        });

        var items = new List<IUsedInItem>();
        await foreach (var item in client.GetItemUsedIn("my_item")
                           .Where(f => f.System("type").IsEqualTo("article"))
                           .EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.Single(items);
        Assert.Equal("parent_1", items[0].System.Codename);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task AssetUsedIn_Where_SendsFilterInQueryString()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/assets/my_asset/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .WithQueryString("system.type[eq]", "article")
            .Respond(_ => CreateUsedInResponse(["parent_1"], continuationToken: null));

        var client = BuildClient(env, mockHttp, new DeliveryOptions
        {
            EnvironmentId = env,
            EnableResilience = false
        });

        var items = new List<IUsedInItem>();
        await foreach (var item in client.GetAssetUsedIn("my_asset")
                           .Where(f => f.System("type").IsEqualTo("article"))
                           .EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.Single(items);
        Assert.Equal("parent_1", items[0].System.Codename);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ItemUsedIn_MultipleWhereConditions_AllFiltersApplied()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/items/my_item/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .WithQueryString("system.type[eq]", "article")
            .WithQueryString("system.language[eq]", "en-US")
            .Respond(_ => CreateUsedInResponse(["parent_1"], continuationToken: null));

        var client = BuildClient(env, mockHttp, new DeliveryOptions
        {
            EnvironmentId = env,
            EnableResilience = false
        });

        var items = new List<IUsedInItem>();
        await foreach (var item in client.GetItemUsedIn("my_item")
                           .Where(f => f
                               .System("type").IsEqualTo("article")
                               .System("language").IsEqualTo("en-US"))
                           .EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.Single(items);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ItemUsedIn_WhereWithPagination_FiltersAppliedToAllPages()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/items/my_item/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .WithQueryString("system.type[eq]", "article")
            .Respond(_ => CreateUsedInResponse(["parent_1"], continuationToken: "token_1"));

        mockHttp.Expect(usedInUrl)
            .WithHeaders("X-Continuation", "token_1")
            .WithQueryString("system.type[eq]", "article")
            .Respond(_ => CreateUsedInResponse(["parent_2"], continuationToken: null));

        var client = BuildClient(env, mockHttp, new DeliveryOptions
        {
            EnvironmentId = env,
            EnableResilience = false
        });

        var items = new List<IUsedInItem>();
        await foreach (var item in client.GetItemUsedIn("my_item")
                           .Where(f => f.System("type").IsEqualTo("article"))
                           .EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.Equal(["parent_1", "parent_2"], items.Select(x => x.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    private static IDeliveryClient BuildClient(
        string env,
        MockHttpMessageHandler mockHttp,
        DeliveryOptions? options = null,
        ILoggerProvider? loggerProvider = null)
    {
        var services = new ServiceCollection();
        if (loggerProvider is not null)
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddProvider(loggerProvider);
            });
        }

        options ??= new DeliveryOptions { EnvironmentId = env };
        services.AddDeliveryClient(
            options,
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    private static HttpResponseMessage CreateUsedInResponse(IReadOnlyList<string> codenames, string? continuationToken)
    {
        var itemsJson = string.Join(",", codenames.Select(codename =>
            $"{{\"system\":{{\"id\":\"{Guid.NewGuid()}\",\"name\":\"{codename}\",\"codename\":\"{codename}\",\"type\":\"article\",\"last_modified\":\"2024-01-01T00:00:00Z\",\"language\":\"en-US\",\"collection\":\"default\",\"workflow\":\"default\",\"workflow_step\":\"published\"}}}}"));

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"items\":[{itemsJson}]}}", Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(continuationToken))
            response.Headers.Add("X-Continuation", continuationToken);

        return response;
    }

    private sealed record LogEntry(int EventId, string Message);

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries => _entries;

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(_entries);

        public void Dispose()
        {
        }
    }

    private sealed class CollectingLogger(List<LogEntry> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => entries.Add(new LogEntry(eventId.Id, formatter(state, exception)));
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
