using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.RetryPolicy;

public class DefaultRetryPolicyTests
{
    [Fact]
    public async Task HttpClient_Success_DoesNotRetry()
    {
        var env = Guid.NewGuid();
        var mockHttp = new MockHttpMessageHandler();

        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        var itemsJson = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json"));
        mockHttp.When(itemsUrl).Respond("application/json", itemsJson);

        var behavior = new TestBehaviorHandler();
        var client = BuildClient(env, mockHttp, behavior, b =>
        {
            b.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = args =>
                {
                    if (args.Outcome.Exception is HttpRequestException)
                        return ValueTask.FromResult(true);
                    if (args.Outcome.Result is HttpResponseMessage rsp)
                    {
                        var sc = (int)rsp.StatusCode;
                        if (sc == 408 || sc >= 500)
                            return ValueTask.FromResult(true);
                    }
                    return ValueTask.FromResult(false);
                }
            });
        });

        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, behavior.Attempts);
    }

    [Fact]
    public async Task HttpClient_RecoversAfterServerError()
    {
        var env = Guid.NewGuid();
        var mockHttp = new MockHttpMessageHandler();

        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        var itemsJson = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json"));

        mockHttp.Expect(HttpMethod.Get, itemsUrl)
                .Respond(HttpStatusCode.InternalServerError);

        mockHttp.Expect(HttpMethod.Get, itemsUrl)
                .Respond("application/json", itemsJson);

        var behavior = new TestBehaviorHandler();
        var client = BuildClient(env, mockHttp, behavior, b =>
        {
            b.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = args =>
                {
                    if (args.Outcome.Exception is HttpRequestException)
                        return ValueTask.FromResult(true);
                    if (args.Outcome.Result is HttpResponseMessage rsp)
                    {
                        var sc = (int)rsp.StatusCode;
                        if (sc == 408 || sc >= 500)
                            return ValueTask.FromResult(true);
                    }
                    return ValueTask.FromResult(false);
                }
            });
        });

        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, behavior.Attempts);
    }

    [Fact]
    public async Task HttpClient_RetriesUpToConfiguredLimit_ThenFails()
    {
        var env = Guid.NewGuid();
        var mockHttp = new MockHttpMessageHandler();

        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        mockHttp.When(itemsUrl).Respond(HttpStatusCode.InternalServerError);

        var behavior = new TestBehaviorHandler();
        var maxAttempts = 2;
        var client = BuildClient(env, mockHttp, behavior, b =>
        {
            b.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = maxAttempts,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = args =>
                {
                    if (args.Outcome.Exception is HttpRequestException)
                        return ValueTask.FromResult(true);
                    if (args.Outcome.Result is HttpResponseMessage rsp)
                    {
                        var sc = (int)rsp.StatusCode;
                        if (sc == 408 || sc >= 500)
                            return ValueTask.FromResult(true);
                    }
                    return ValueTask.FromResult(false);
                }
            });
        });

        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();

        Assert.False(result.IsSuccess);
        // total tries = initial + retries
        Assert.Equal(1 + maxAttempts, behavior.Attempts);
    }

    [Fact]
    public async Task HttpClient_RetriesAfterException_ThenSucceeds()
    {
        var env = Guid.NewGuid();
        var mockHttp = new MockHttpMessageHandler();

        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        var itemsJson = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json"));
        mockHttp.When(itemsUrl).Respond("application/json", itemsJson);

        var behavior = new TestBehaviorHandler(throwsOnFirstAttempt: true);
        var client = BuildClient(env, mockHttp, behavior, b =>
        {
            b.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = args =>
                {
                    if (args.Outcome.Exception is HttpRequestException)
                        return ValueTask.FromResult(true);
                    if (args.Outcome.Result is HttpResponseMessage rsp)
                    {
                        var sc = (int)rsp.StatusCode;
                        if (sc == 408 || sc >= 500)
                            return ValueTask.FromResult(true);
                    }
                    return ValueTask.FromResult(false);
                }
            });
        });

        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, behavior.Attempts);
    }

    private static DeliveryClient BuildClient(
        Guid environmentId,
        MockHttpMessageHandler primary,
        TestBehaviorHandler behaviorHandler,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>> configureResilience)
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = environmentId.ToString(), EnableResilience = true },
            configureRefit: null,
            configureHttpClient: builder =>
            {
                builder.ConfigurePrimaryHttpMessageHandler(() => primary);
                builder.AddHttpMessageHandler(() => behaviorHandler);
            },
            configureResilience: configureResilience);

        var provider = services.BuildServiceProvider();
        return (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();
    }

    [Fact]
    public async Task HttpClient_RateLimited_RespectsRetryAfterHeader()
    {
        var env = Guid.NewGuid();
        var mockHttp = new MockHttpMessageHandler();

        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        var itemsJson = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json"));

        // First request returns 429 with Retry-After header (seconds until retry allowed)
        mockHttp.Expect(HttpMethod.Get, itemsUrl)
            .Respond(req =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(100));
                return response;
            });

        // Second request succeeds
        mockHttp.Expect(HttpMethod.Get, itemsUrl)
            .Respond("application/json", itemsJson);

        var behavior = new TestBehaviorHandler();
        var client = BuildClientWithDefaultResilience(env, mockHttp, behavior);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();
        stopwatch.Stop();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, behavior.Attempts);
        // Should have waited at least 100ms (the Retry-After value)
        Assert.True(stopwatch.ElapsedMilliseconds >= 90, $"Expected delay of at least 90ms, but was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task HttpClient_RateLimited_WithoutRetryAfter_UsesExponentialBackoff()
    {
        var env = Guid.NewGuid();
        var mockHttp = new MockHttpMessageHandler();

        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        var itemsJson = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json"));

        // First request returns 429 without Retry-After header
        mockHttp.Expect(HttpMethod.Get, itemsUrl)
            .Respond(HttpStatusCode.TooManyRequests);

        // Second request succeeds
        mockHttp.Expect(HttpMethod.Get, itemsUrl)
            .Respond("application/json", itemsJson);

        var behavior = new TestBehaviorHandler();
        var client = BuildClientWithDefaultResilience(env, mockHttp, behavior);

        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, behavior.Attempts);
    }

    private static DeliveryClient BuildClientWithDefaultResilience(
        Guid environmentId,
        MockHttpMessageHandler primary,
        TestBehaviorHandler behaviorHandler)
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = environmentId.ToString(), EnableResilience = true },
            configureRefit: null,
            configureHttpClient: builder =>
            {
                builder.ConfigurePrimaryHttpMessageHandler(() => primary);
                builder.AddHttpMessageHandler(() => behaviorHandler);
            },
            configureResilience: null); // Use default resilience configuration

        var provider = services.BuildServiceProvider();
        return (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();
    }

    private class TestBehaviorHandler(bool throwsOnFirstAttempt = false) : DelegatingHandler
    {
        private readonly bool _throwsOnFirstAttempt = throwsOnFirstAttempt;

        public int Attempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            if (_throwsOnFirstAttempt && Attempts == 1)
            {
                throw new HttpRequestException("Simulated network failure");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
