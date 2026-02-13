using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Kontent.Ai.Delivery;

public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers and configures a named HTTP client with Refit.
    /// </summary>
    private static void RegisterNamedHttpClient(
        IServiceCollection services,
        string name,
        Action<IHttpClientBuilder>? configureHttpClient,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience,
        Action<RefitSettings>? configureRefit)
    {
        var refitSettings = CreateRefitSettings(configureRefit);

        var httpClientName = GetHttpClientName(name);
        var httpClientBuilder = services
            .AddHttpClient(httpClientName)
            .ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
                var options = optionsMonitor.Get(name);
                // Note: BaseAddress is static and won't update with runtime configuration changes.
                // The DeliveryAuthenticationHandler handles runtime endpoint switching.
                httpClient.BaseAddress = new Uri(options.GetBaseUrl(), UriKind.Absolute);
            });

        // Add resilience and message handlers
        ConfigureResilienceHandler(httpClientBuilder, $"delivery_{name}", name, configureResilience);
        AddMessageHandlers(httpClientBuilder, name);

        // Apply custom configuration
        configureHttpClient?.Invoke(httpClientBuilder);

        // Register keyed IDeliveryApi - create Refit client from the configured HTTP pipeline
        services.AddKeyedTransient(name, (sp, _) =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(httpClientName);
            return RestService.For<IDeliveryApi>(httpClient, refitSettings);
        });
    }

    /// <summary>
    /// Creates and configures Refit settings with optional customization.
    /// </summary>
    private static RefitSettings CreateRefitSettings(Action<RefitSettings>? configureRefit)
    {
        var refitSettings = RefitSettingsProvider.CreateDefaultSettings();
        configureRefit?.Invoke(refitSettings);
        return refitSettings;
    }

    /// <summary>
    /// Configures the resilience handler for an HTTP client.
    /// </summary>
    /// <param name="httpClientBuilder">The HTTP client builder.</param>
    /// <param name="resilienceHandlerName">The name of the resilience handler.</param>
    /// <param name="clientName">The name of the named client options.</param>
    /// <param name="configureResilience">Optional custom resilience configuration.</param>
    private static void ConfigureResilienceHandler(
        IHttpClientBuilder httpClientBuilder,
        string resilienceHandlerName,
        string clientName,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience)
    {
        httpClientBuilder.AddResilienceHandler(resilienceHandlerName, (builder, context) =>
        {
            var optionsMonitor = context.ServiceProvider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
            var options = optionsMonitor.Get(clientName);

            if (!options.EnableResilience)
                return;

            if (configureResilience is not null)
            {
                configureResilience(builder);
            }
            else
            {
                ConfigureDefaultResilience(builder);
            }
        });
    }

    /// <summary>
    /// Adds tracking and authentication message handlers to an HTTP client.
    /// </summary>
    /// <param name="httpClientBuilder">The HTTP client builder.</param>
    /// <param name="clientName">The name of the options for the authentication handler.</param>
    private static void AddMessageHandlers(IHttpClientBuilder httpClientBuilder, string clientName)
    {
        httpClientBuilder.AddHttpMessageHandler(sp => new TrackingHandler(
            sp.GetService<ILogger<TrackingHandler>>()));

        httpClientBuilder.AddHttpMessageHandler(sp => new DeliveryAuthenticationHandler(
            sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>(),
            clientName,
            sp.GetService<ILogger<DeliveryAuthenticationHandler>>()));
    }

    private static void ConfigureDefaultResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        // Retry policy with Retry-After header support
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = args => ValueTask.FromResult(
                IsTransientException(args.Outcome.Exception, args.Context.CancellationToken) ||
                (args.Outcome.Result?.IsSuccessStatusCode == false &&
                 IsRetryableStatusCode(args.Outcome.Result?.StatusCode))),
            DelayGenerator = GetRetryAfterDelay
        });

        builder.AddTimeout(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Extracts the retry delay from the Retry-After header on 429 responses.
    /// Kontent.ai returns Retry-After as seconds until the next request is allowed.
    /// </summary>
    private static ValueTask<TimeSpan?> GetRetryAfterDelay(RetryDelayGeneratorArguments<HttpResponseMessage> args)
    {
        if (args.Outcome.Result is { StatusCode: System.Net.HttpStatusCode.TooManyRequests } response
            && response.Headers.RetryAfter?.Delta is { } retryAfter)
        {
            return ValueTask.FromResult<TimeSpan?>(retryAfter);
        }

        // Fall back to default exponential backoff
        return ValueTask.FromResult<TimeSpan?>(null);
    }

    private static bool IsRetryableStatusCode(System.Net.HttpStatusCode? statusCode)
        => statusCode is
            System.Net.HttpStatusCode.TooManyRequests or
            System.Net.HttpStatusCode.RequestTimeout or
            System.Net.HttpStatusCode.InternalServerError or
            System.Net.HttpStatusCode.BadGateway or
            System.Net.HttpStatusCode.ServiceUnavailable or
            System.Net.HttpStatusCode.GatewayTimeout;

    private static bool IsTransientException(Exception? exception, CancellationToken requestCancellationToken)
    {
        if (exception is null)
            return false;

        if (exception is OperationCanceledException)
        {
            // Respect caller cancellations - these should never be retried.
            if (requestCancellationToken.IsCancellationRequested)
                return false;

            // HttpClient timeouts commonly surface as TaskCanceledException.
            return exception is TaskCanceledException || exception.InnerException is TimeoutException;
        }

        return exception is System.Net.Http.HttpRequestException or TimeoutException;
    }
}
