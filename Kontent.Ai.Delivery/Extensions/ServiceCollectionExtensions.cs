using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Handlers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Kontent.Ai.Delivery.Extensions;

/// <summary>
/// Extension methods for registering Kontent.ai Delivery SDK services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Kontent.ai Delivery client with the specified options instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="deliveryOptions">The delivery options instance.</param>
    /// <param name="configureHttpClient">Optional action to configure the HTTP client.</param>
    /// <param name="configureResilience">Optional action to configure resilience policies.</param>
    /// <param name="configureRefit">Optional action to configure Refit settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        DeliveryOptions deliveryOptions,
        Action<IHttpClientBuilder>? configureHttpClient = null,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        ArgumentNullException.ThrowIfNull(deliveryOptions);

        return services
            .RegisterOptions(deliveryOptions)
            .RegisterServices(configureHttpClient, configureResilience, configureRefit);
    }

    /// <summary>
    /// Registers the Kontent.ai Delivery client with the specified options builder.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="buildDeliveryOptions">A function to build the delivery options.</param>
    /// <param name="configureHttpClient">Optional action to configure the HTTP client.</param>
    /// <param name="configureResilience">Optional action to configure resilience policies.</param>
    /// <param name="configureRefit">Optional action to configure Refit settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions,
        Action<IHttpClientBuilder>? configureHttpClient = null,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        ArgumentNullException.ThrowIfNull(buildDeliveryOptions);

        var builder = DeliveryOptionsBuilder.CreateInstance();
        var options = buildDeliveryOptions(builder);

        return services
            .RegisterOptions(options)
            .RegisterServices(configureHttpClient, configureResilience, configureRefit);
    }

    /// <summary>
    /// Registers the Kontent.ai Delivery client using configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configurationSectionName">The configuration section name. Defaults to "DeliveryOptions".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "DeliveryOptions")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = string.IsNullOrWhiteSpace(configurationSectionName)
            ? configuration
            : configuration.GetSection(configurationSectionName);


        return services
            .Configure<DeliveryOptions>(section)
            .RegisterServices();
    }

    /// <summary>
    /// Registers the Kontent.ai Delivery client with configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the delivery options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        Action<DeliveryOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services
            .Configure(configureOptions)
            .RegisterServices();
    }

    /// <summary>
    /// Registers the Kontent.ai Delivery client with advanced configuration options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the delivery options.</param>
    /// <param name="configureHttpClient">Optional action to configure the HTTP client.</param>
    /// <param name="configureResilience">Optional action to configure resilience policies.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        Action<DeliveryOptions> configureOptions,
        Action<IHttpClientBuilder>? configureHttpClient,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services
            .Configure(configureOptions)
            .RegisterServices(configureHttpClient, configureResilience);
    }

    /// <summary>
    /// Core registration method containing all service registrations.
    /// </summary>
    private static IServiceCollection RegisterServices(
        this IServiceCollection services,
        Action<IHttpClientBuilder>? configureHttpClient = null,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        services.AddOptions<DeliveryOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RegisterDependencies(services);
        RegisterHttpClient(services, configureHttpClient, configureResilience, configureRefit);

        services.TryAddSingleton<IDeliveryClient, DeliveryClient>();

        return services;
    }

    private static void RegisterDependencies(IServiceCollection services)
    {
        // JSON serialization
        services.TryAddSingleton(RefitSettingsProvider.CreateDefaultJsonSerializerOptions());

        // HTTP handlers
        services.TryAddTransient<TrackingHandler>();
        services.TryAddTransient<DeliveryAuthenticationHandler>();

        // Core services
        services.TryAddSingleton<IPropertyMapper, PropertyMapper>();
        services.TryAddSingleton<ITypeProvider, TypeProvider>();
        services.TryAddSingleton<IItemTypingStrategy, DefaultItemTypingStrategy>();
        services.TryAddSingleton<IContentDeserializer, ContentDeserializer>();
        services.TryAddSingleton<IElementsPostProcessor, ElementsPostProcessor>();
        services.TryAddSingleton<IHtmlParser, HtmlParser>();
    }

    /// <summary>
    /// Registers and configures the HTTP client with Refit.
    /// </summary>
    private static void RegisterHttpClient(
        IServiceCollection services,
        Action<IHttpClientBuilder>? configureHttpClient,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience,
        Action<RefitSettings>? configureRefit)
    {
        var refitSettings = RefitSettingsProvider.CreateDefaultSettings();
        configureRefit?.Invoke(refitSettings);

        var httpClientBuilder = services
            .AddRefitClient<IDeliveryApi>(refitSettings)
            .ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>().CurrentValue;
                httpClient.BaseAddress = new Uri(options.GetBaseUrl(), UriKind.Absolute);
            });

        // Add resilience handler
        httpClientBuilder.AddResilienceHandler("delivery", (builder, context) =>
        {
            var options = context.ServiceProvider
                .GetRequiredService<IOptionsMonitor<DeliveryOptions>>()
                .CurrentValue;

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

        // Add message handlers
        httpClientBuilder
            .AddHttpMessageHandler<TrackingHandler>()
            .AddHttpMessageHandler<DeliveryAuthenticationHandler>();

        // Apply custom configuration
        configureHttpClient?.Invoke(httpClientBuilder);
    }

    private static IServiceCollection RegisterOptions(this IServiceCollection services, DeliveryOptions options) =>
        services.Configure<DeliveryOptions>(o => o.Configure(options));

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

    /// <summary>
    /// Enables in-memory caching for the Delivery Client using <see cref="IMemoryCache"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultExpiration">
    /// Default cache entry expiration time. If null, defaults to 1 hour.
    /// Individual queries can override this using query parameters.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description><see cref="IMemoryCache"/> - The underlying memory cache (if not already registered)</description></item>
    /// <item><description><see cref="IDeliveryCacheManager"/> - Memory cache manager implementation</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IServiceCollection WithMemoryCache(
        this IServiceCollection services,
        TimeSpan? defaultExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register IMemoryCache if not already registered
        services.AddMemoryCache();

        // Register the cache manager as a singleton
        services.TryAddSingleton<IDeliveryCacheManager>(sp =>
            new MemoryCacheManager(
                sp.GetRequiredService<IMemoryCache>(),
                defaultExpiration));

        return services;
    }

    /// <summary>
    /// Enables distributed caching for the Delivery Client using <see cref="IDistributedCache"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultExpiration">
    /// Default cache entry expiration time. If null, defaults to 1 hour.
    /// Individual queries can override this using query parameters.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description><see cref="IDeliveryCacheManager"/> - Distributed cache manager implementation</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> You must register an <see cref="IDistributedCache"/> implementation before calling this method.
    /// Common implementations:
    /// <list type="bullet">
    /// <item><description>Redis: <c>services.AddStackExchangeRedisCache(options => ...)</c></description></item>
    /// <item><description>SQL Server: <c>services.AddDistributedSqlServerCache(options => ...)</c></description></item>
    /// <item><description>NCache: <c>services.AddNCacheDistributedCache(options => ...)</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IDistributedCache"/> implementation is registered.
    /// </exception>
    public static IServiceCollection WithDistributedCache(
        this IServiceCollection services,
        TimeSpan? defaultExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the cache manager as a singleton
        // The IDistributedCache dependency will be resolved from services
        // If it's not registered, this will fail at runtime with a clear error
        services.TryAddSingleton<IDeliveryCacheManager>(sp =>
            new DistributedCacheManager(
                sp.GetRequiredService<IDistributedCache>(),
                defaultExpiration));

        return services;
    }
}