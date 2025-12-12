using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Kontent.Ai.Delivery.Extensions;

public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers and configures a named HTTP client with Refit.
    /// </summary>
    private static void RegisterNamedHttpClient(
        IServiceCollection services,
        string name,
        Action<IHttpClientBuilder>? configureHttpClient,
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience,
        Action<RefitSettings>? configureRefit)
    {
        var refitSettings = CreateRefitSettings(configureRefit);

        // Register named HTTP client with unique name to avoid conflicts
        var httpClientName = $"Kontent.Ai.Delivery.HttpClient.{name}";
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

        // Bind Refit client to the HTTP pipeline using typed client pattern
        httpClientBuilder.AddTypedClient(http => RestService.For<IDeliveryApi>(http, refitSettings));

        // Apply custom configuration
        configureHttpClient?.Invoke(httpClientBuilder);

        // Register keyed IDeliveryApi - retrieve from HTTP client factory
        // The typed client is created and managed by the HTTP pipeline
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
    /// <param name="optionsName">The name of the options to retrieve, or null for default options.</param>
    /// <param name="configureResilience">Optional custom resilience configuration.</param>
    private static void ConfigureResilienceHandler(
        IHttpClientBuilder httpClientBuilder,
        string resilienceHandlerName,
        string? optionsName,
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience)
    {
        httpClientBuilder.AddResilienceHandler(resilienceHandlerName, (builder, context) =>
        {
            var optionsMonitor = context.ServiceProvider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
            var options = optionsName is null
                ? optionsMonitor.CurrentValue
                : optionsMonitor.Get(optionsName);

            if (!options.EnableResilience)
                return;

            if (configureResilience != null)
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
    /// <param name="optionsName">The name of the options for the authentication handler, or null for default options.</param>
    private static void AddMessageHandlers(IHttpClientBuilder httpClientBuilder, string? optionsName)
    {
        // Tracking handler with optional logger
        httpClientBuilder.AddHttpMessageHandler(sp => new TrackingHandler(
            sp.GetService<ILogger<TrackingHandler>>()));

        if (optionsName is null)
        {
            // Default options - use parameterless constructor with optional logger
            httpClientBuilder.AddHttpMessageHandler(sp => new DeliveryAuthenticationHandler(
                sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>(),
                sp.GetService<ILogger<DeliveryAuthenticationHandler>>()));
        }
        else
        {
            // Named options - pass name to constructor with optional logger
            httpClientBuilder.AddHttpMessageHandler(sp => new DeliveryAuthenticationHandler(
                sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>(),
                optionsName,
                sp.GetService<ILogger<DeliveryAuthenticationHandler>>()));
        }
    }

    private static void ConfigureDefaultResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        // Retry policy
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result?.IsSuccessStatusCode == false &&
                IsRetryableStatusCode(args.Outcome.Result?.StatusCode))
        });

        // Timeout policy
        builder.AddTimeout(TimeSpan.FromSeconds(30));
    }

    private static bool IsRetryableStatusCode(System.Net.HttpStatusCode? statusCode)
        => statusCode is
            System.Net.HttpStatusCode.TooManyRequests or
            System.Net.HttpStatusCode.RequestTimeout or
            System.Net.HttpStatusCode.InternalServerError or
            System.Net.HttpStatusCode.BadGateway or
            System.Net.HttpStatusCode.ServiceUnavailable or
            System.Net.HttpStatusCode.GatewayTimeout;
}


