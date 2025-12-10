using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Processing;
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
    private static readonly object _registryLock = new();

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

        return services.AddDeliveryClient(
            Abstractions.Options.DefaultName,
            options => options.Configure(deliveryOptions),
            configureHttpClient,
            configureResilience,
            configureRefit);
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

        return services.AddDeliveryClient(
            Abstractions.Options.DefaultName,
            opts => opts.Configure(options),
            configureHttpClient,
            configureResilience,
            configureRefit);
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

        return services.AddDeliveryClient(
            Abstractions.Options.DefaultName,
            section.Bind);
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

        return services.AddDeliveryClient(
            Abstractions.Options.DefaultName,
            configureOptions);
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
        return services.AddDeliveryClient(
            Abstractions.Options.DefaultName,
            configureOptions,
            configureHttpClient,
            configureResilience);
    }

    /// <summary>
    /// Registers a named Kontent.ai Delivery client with the specified configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The registered client can be accessed in two ways:
    /// <list type="bullet">
    /// <item>Via <see cref="IDeliveryClientFactory"/>: <c>factory.Get("name")</c></item>
    /// <item>Via keyed services injection: <c>[FromKeyedServices("name")] IDeliveryClient client</c></item>
    /// </list>
    /// </para>
    /// <para>
    /// The client supports reactive configuration updates via <see cref="IOptionsMonitor{TOptions}"/>.
    /// Changes to API keys and other options will be picked up automatically at runtime.
    /// </para>
    /// <para>
    /// Note: The HTTP client's BaseAddress is set once during initialization and will not update
    /// with runtime configuration changes. However, the authentication handler monitors options
    /// changes to support scenarios like API key rotation and endpoint switching.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the client. Must be unique across all registrations.</param>
    /// <param name="configureOptions">Action to configure the delivery options.</param>
    /// <param name="configureHttpClient">Optional action to configure the HTTP client.</param>
    /// <param name="configureResilience">Optional action to configure resilience policies.</param>
    /// <param name="configureRefit">Optional action to configure Refit settings.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a client with the same name is already registered.</exception>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        string name,
        Action<DeliveryOptions> configureOptions,
        Action<IHttpClientBuilder>? configureHttpClient = null,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Validate name doesn't contain only whitespace
        if (string.IsNullOrWhiteSpace(name) || name.Trim() != name || name.Contains(' '))
        {
            throw new ArgumentException(
                "Client name cannot be empty, contain leading/trailing whitespace, or contain spaces. Use underscores or hyphens instead.",
                nameof(name));
        }

        // Validate name uniqueness
        var registry = GetOrCreateRegistry(services);
        if (!registry.TryRegister(name))
        {
            var httpClientName = $"Kontent.Ai.Delivery.HttpClient.{name}";
            throw new InvalidOperationException(
                $"A DeliveryClient with the name '{name}' has already been registered. " +
                $"HTTP client name: '{httpClientName}'. Each client must have a unique name.");
        }

        // Register named options
        services.Configure(name, configureOptions);
        services.AddOptions<DeliveryOptions>(name)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Also configure unnamed options for backward compatibility if this is the default name
        if (name == Abstractions.Options.DefaultName)
        {
            services.Configure(configureOptions);
            services.AddOptions<DeliveryOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        // Register dependencies (only once)
        RegisterDependencies(services);

        // Register named HTTP client and Refit API
        RegisterNamedHttpClient(services, name, configureHttpClient, configureResilience, configureRefit);

        // Register keyed IDeliveryClient
        services.AddKeyedSingleton<IDeliveryClient>(name, (sp, key) =>
        {
            var clientName = (string)key!;
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
            var namedMonitor = new NamedOptionsMonitor<DeliveryOptions>(optionsMonitor, clientName);

            var deliveryApi = sp.GetRequiredKeyedService<IDeliveryApi>(clientName);
            var elementsPostProcessor = sp.GetRequiredService<IElementsPostProcessor>();

            // Resolve keyed cache manager for this client, with fallback to global for backward compatibility
            // New per-client caching: AddDeliveryMemoryCache(clientName)
            // Deprecated global caching: WithMemoryCache() - falls back to non-keyed cache manager
            var cacheManager = sp.GetKeyedService<IDeliveryCacheManager>(clientName)
                ?? sp.GetService<IDeliveryCacheManager>();

            return new DeliveryClient(
                deliveryApi,
                namedMonitor,
                elementsPostProcessor,
                cacheManager);
        });

        // Register factory
        services.TryAddSingleton<IDeliveryClientFactory, DeliveryClientFactory>();

        // Register default client accessors if this is the default name (backward compatibility)
        if (name == Abstractions.Options.DefaultName)
        {
            services.TryAddSingleton(sp =>
                sp.GetRequiredKeyedService<IDeliveryApi>(Abstractions.Options.DefaultName));

            services.TryAddSingleton(sp =>
                sp.GetRequiredKeyedService<IDeliveryClient>(Abstractions.Options.DefaultName));
        }

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

        // Dependency extraction - default to no-op when caching is disabled
        services.TryAddSingleton<IContentDependencyExtractor>(NullContentDependencyExtractor.Instance);
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
        var refitSettings = CreateRefitSettings(configureRefit);

        var httpClientBuilder = services
            .AddRefitClient<IDeliveryApi>(refitSettings)
            .ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>().CurrentValue;
                httpClient.BaseAddress = new Uri(options.GetBaseUrl(), UriKind.Absolute);
            });

        // Add resilience and message handlers
        ConfigureResilienceHandler(httpClientBuilder, "delivery", optionsName: null, configureResilience);
        AddMessageHandlers(httpClientBuilder, optionsName: null);

        // Apply custom configuration
        configureHttpClient?.Invoke(httpClientBuilder);
    }

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
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience)
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
        httpClientBuilder.AddHttpMessageHandler<TrackingHandler>();

        if (optionsName is null)
        {
            // Default options - use parameterless constructor
            httpClientBuilder.AddHttpMessageHandler<DeliveryAuthenticationHandler>();
        }
        else
        {
            // Named options - pass name to constructor
            httpClientBuilder.AddHttpMessageHandler(sp => new DeliveryAuthenticationHandler(
                sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>(),
                optionsName));
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
    [Obsolete("Use AddDeliveryMemoryCache(clientName) for per-client caching. " +
              "Global caching applies to all clients and will be removed in a future version.")]
    public static IServiceCollection WithMemoryCache(
        this IServiceCollection services,
        TimeSpan? defaultExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register IMemoryCache if not already registered
        services.AddMemoryCache();

        // Register the cache manager as a singleton (only if not already registered)
        // This prevents accidental overwrites if called multiple times
        services.TryAddSingleton<IDeliveryCacheManager>(sp =>
            new MemoryCacheManager(
                sp.GetRequiredService<IMemoryCache>(),
                keyPrefix: null,
                defaultExpiration));

        // Override default no-op extractor with actual implementation
        // Use TryAddSingleton to avoid replacing if already customized
        services.TryAddSingleton<IContentDependencyExtractor, ContentDependencyExtractor>();

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
    [Obsolete("Use AddDeliveryDistributedCache(clientName) for per-client caching. " +
              "Global caching applies to all clients and will be removed in a future version.")]
    public static IServiceCollection WithDistributedCache(
        this IServiceCollection services,
        TimeSpan? defaultExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the cache manager as a singleton (only if not already registered)
        // The IDistributedCache dependency will be resolved from services
        // If it's not registered, this will fail at runtime with a clear error
        // This prevents accidental overwrites if called multiple times
        services.TryAddSingleton<IDeliveryCacheManager>(sp =>
            new DistributedCacheManager(
                sp.GetRequiredService<IDistributedCache>(),
                keyPrefix: null,
                defaultExpiration));

        // Override default no-op extractor with actual implementation
        // Use TryAddSingleton to avoid replacing if already customized
        services.TryAddSingleton<IContentDependencyExtractor, ContentDependencyExtractor>();

        return services;
    }

    /// <summary>
    /// Registers a memory cache manager for a specific named Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="keyPrefix">
    /// Optional prefix for cache keys. If null, defaults to the client name.
    /// Used to isolate cache entries when multiple clients share the same <see cref="IMemoryCache"/>.
    /// </param>
    /// <param name="defaultExpiration">
    /// Default cache entry expiration time. If null, defaults to 1 hour.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers a keyed <see cref="IDeliveryCacheManager"/> for the specified client.
    /// The cache manager is only used by the named client - other clients without a keyed
    /// cache manager registration will not use caching.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// services.AddDeliveryClient("production", o => o.EnvironmentId = prodEnvId);
    /// services.AddDeliveryMemoryCache("production", defaultExpiration: TimeSpan.FromHours(2));
    ///
    /// services.AddDeliveryClient("preview", o => { ... }); // No caching for preview
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryMemoryCache(
        this IServiceCollection services,
        string clientName,
        string? keyPrefix = null,
        TimeSpan? defaultExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        // Register IMemoryCache if not already registered (shared across all clients)
        services.AddMemoryCache();

        // Register keyed cache manager for this client
        services.AddKeyedSingleton<IDeliveryCacheManager>(clientName, (sp, _) =>
            new MemoryCacheManager(
                sp.GetRequiredService<IMemoryCache>(),
                keyPrefix ?? clientName,
                defaultExpiration));

        // Enable dependency extraction for cache invalidation
        // Replace ensures real extractor is used regardless of registration order
        services.Replace(ServiceDescriptor.Singleton<IContentDependencyExtractor, ContentDependencyExtractor>());

        return services;
    }

    /// <summary>
    /// Registers a distributed cache manager for a specific named Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="keyPrefix">
    /// Optional prefix for cache keys. If null, defaults to the client name.
    /// Used to isolate cache entries when multiple clients share the same <see cref="IDistributedCache"/>.
    /// </param>
    /// <param name="defaultExpiration">
    /// Default cache entry expiration time. If null, defaults to 1 hour.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers a keyed <see cref="IDeliveryCacheManager"/> for the specified client.
    /// The cache manager is only used by the named client - other clients without a keyed
    /// cache manager registration will not use caching.
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> You must register an <see cref="IDistributedCache"/> implementation before calling this method.
    /// Common implementations:
    /// <list type="bullet">
    /// <item><description>Redis: <c>services.AddStackExchangeRedisCache(options => ...)</c></description></item>
    /// <item><description>SQL Server: <c>services.AddDistributedSqlServerCache(options => ...)</c></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");
    /// services.AddDeliveryClient("production", o => o.EnvironmentId = prodEnvId);
    /// services.AddDeliveryDistributedCache("production", defaultExpiration: TimeSpan.FromHours(2));
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryDistributedCache(
        this IServiceCollection services,
        string clientName,
        string? keyPrefix = null,
        TimeSpan? defaultExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        // Register keyed cache manager for this client
        services.AddKeyedSingleton<IDeliveryCacheManager>(clientName, (sp, _) =>
            new DistributedCacheManager(
                sp.GetRequiredService<IDistributedCache>(),
                keyPrefix ?? clientName,
                defaultExpiration));

        // Enable dependency extraction for cache invalidation
        // Replace ensures real extractor is used regardless of registration order
        services.Replace(ServiceDescriptor.Singleton<IContentDependencyExtractor, ContentDependencyExtractor>());

        return services;
    }

    /// <summary>
    /// Gets or creates the singleton DeliveryClientRegistry instance.
    /// Thread-safe to prevent race conditions during concurrent registrations.
    /// </summary>
    private static DeliveryClientRegistry GetOrCreateRegistry(IServiceCollection services)
    {
        lock (_registryLock)
        {
            var descriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(DeliveryClientRegistry));

            if (descriptor?.ImplementationInstance is DeliveryClientRegistry existing)
            {
                return existing;
            }

            var registry = new DeliveryClientRegistry();
            services.AddSingleton(registry);
            return registry;
        }
    }
}