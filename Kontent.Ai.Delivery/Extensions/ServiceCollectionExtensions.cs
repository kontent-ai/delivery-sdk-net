using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Extension methods for registering Kontent.ai Delivery SDK services.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    private const string HttpClientNamePrefix = "Kontent.Ai.Delivery.HttpClient.";

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
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        ArgumentNullException.ThrowIfNull(deliveryOptions);

        return services.AddDeliveryClient(
            DeliveryClientNames.Default,
            options => DeliveryOptionsCopyHelper.Copy(source: deliveryOptions, destination: options),
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
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        ArgumentNullException.ThrowIfNull(buildDeliveryOptions);

        var builder = DeliveryOptionsBuilder.CreateInstance();
        var options = buildDeliveryOptions(builder);

        return services.AddDeliveryClient(
            DeliveryClientNames.Default,
            opts => DeliveryOptionsCopyHelper.Copy(source: options, destination: opts),
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
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = string.IsNullOrWhiteSpace(configurationSectionName)
            ? configuration
            : configuration.GetSection(configurationSectionName);

        return services.AddDeliveryClientFromConfiguration(
            DeliveryClientNames.Default,
            section);
    }

    /// <summary>
    /// Registers the Kontent.ai Delivery client using a configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section containing delivery options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        return services.AddDeliveryClientFromConfiguration(
            DeliveryClientNames.Default,
            configurationSection);
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
            DeliveryClientNames.Default,
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
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null)
    {
        return services.AddDeliveryClient(
            DeliveryClientNames.Default,
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
    /// The client supports reactive configuration updates via <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/>.
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
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(name);
        ArgumentNullException.ThrowIfNull(configureOptions);

        EnsureClientNameNotAlreadyRegistered(services, name);

        // Register named options
        services.Configure(name, configureOptions);
        services.AddOptions<DeliveryOptions>(name)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Also configure unnamed options for backward compatibility if this is the default name
        if (name == DeliveryClientNames.Default)
        {
            services.Configure(configureOptions);
            services.AddOptions<DeliveryOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        return CompleteClientRegistration(services, name, configureHttpClient, configureResilience, configureRefit);
    }

    private static IServiceCollection AddDeliveryClientFromConfiguration(
        this IServiceCollection services,
        string name,
        IConfiguration configuration,
        Action<IHttpClientBuilder>? configureHttpClient = null,
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(name);
        ArgumentNullException.ThrowIfNull(configuration);

        EnsureClientNameNotAlreadyRegistered(services, name);

        // Configure named options from configuration with change-token support.
        services.Configure<DeliveryOptions>(name, configuration);
        services.AddOptions<DeliveryOptions>(name)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Also configure unnamed options for backward compatibility if this is the default name.
        if (name == DeliveryClientNames.Default)
        {
            services.Configure<DeliveryOptions>(configuration);
            services.AddOptions<DeliveryOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        return CompleteClientRegistration(services, name, configureHttpClient, configureResilience, configureRefit);
    }

    private static IServiceCollection CompleteClientRegistration(
        IServiceCollection services,
        string name,
        Action<IHttpClientBuilder>? configureHttpClient = null,
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
        Action<RefitSettings>? configureRefit = null)
    {
        // Register dependencies (only once)
        RegisterDependencies(services);

        // Register named HTTP client and Refit API
        RegisterNamedHttpClient(services, name, configureHttpClient, configureResilience, configureRefit);

        // Register keyed IDeliveryClient
        services.AddKeyedSingleton<IDeliveryClient>(name, CreateDeliveryClient);

        // Register factory
        services.TryAddSingleton<IDeliveryClientFactory, DeliveryClientFactory>();

        // Register default client accessors if this is the default name (backward compatibility)
        if (name == DeliveryClientNames.Default)
        {
            services.TryAddSingleton(sp =>
                sp.GetRequiredKeyedService<IDeliveryApi>(DeliveryClientNames.Default));

            services.TryAddSingleton(sp =>
                sp.GetRequiredKeyedService<IDeliveryClient>(DeliveryClientNames.Default));
        }

        return services;
    }

    /// <summary>
    /// Factory method for creating keyed DeliveryClient instances.
    /// </summary>
    private static IDeliveryClient CreateDeliveryClient(IServiceProvider sp, object? key)
    {
        var clientName = (string)key!;

        var deliveryApi = sp.GetRequiredKeyedService<IDeliveryApi>(clientName);
        var contentItemMapper = sp.GetRequiredService<ContentItemMapper>();
        var contentDeserializer = sp.GetRequiredService<IContentDeserializer>();
        var typeProvider = sp.GetRequiredService<ITypeProvider>();

        // Resolve keyed cache manager for this client (registered via AddDeliveryMemoryCache/AddDeliveryDistributedCache/AddDeliveryCacheManager)
        var cacheManager = sp.GetKeyedService<IDeliveryCacheManager>(clientName);
        if (cacheManager is not null)
        {
            cacheManager = new PreviewAwareCacheManager(
                cacheManager,
                sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>(),
                clientName);
        }

        // Resolve logger (optional - will be null if no logging is configured)
        var logger = sp.GetService<ILogger<DeliveryClient>>();

        return new DeliveryClient(
            deliveryApi,
            contentItemMapper,
            contentDeserializer,
            typeProvider,
            cacheManager,
            logger);
    }

    private static string GetHttpClientName(string name) => $"{HttpClientNamePrefix}{name}";

    private static void ValidateClientName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Trim() != name || name.Contains(' '))
        {
            throw new ArgumentException(
                "Client name cannot contain leading/trailing whitespace, or contain spaces. Use underscores or hyphens instead.",
                nameof(name));
        }
    }

    private static void EnsureClientNameNotAlreadyRegistered(IServiceCollection services, string name)
    {
        if (!services.Any(d => d.ServiceType == typeof(IDeliveryClient) && Equals(d.ServiceKey, name)))
        {
            return;
        }

        throw new InvalidOperationException(
            $"A DeliveryClient with the name '{name}' has already been registered. " +
            $"HTTP client name: '{GetHttpClientName(name)}'. Each client must have a unique name.");
    }
}
