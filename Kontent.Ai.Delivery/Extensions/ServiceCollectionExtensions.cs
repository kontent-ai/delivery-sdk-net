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
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
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
    /// Registers the Kontent.ai Delivery client using a configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section containing delivery options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryClient(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(configurationSection);

        return services.AddDeliveryClient(
            Abstractions.Options.DefaultName,
            configurationSection.Bind);
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
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null)
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
        Action<Polly.ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null,
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
        services.AddKeyedSingleton<IDeliveryClient>(name, CreateDeliveryClient);

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

    /// <summary>
    /// Factory method for creating keyed DeliveryClient instances.
    /// </summary>
    private static IDeliveryClient CreateDeliveryClient(IServiceProvider sp, object? key)
    {
        var clientName = (string)key!;
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        var namedMonitor = new NamedOptionsMonitor<DeliveryOptions>(optionsMonitor, clientName);

        var deliveryApi = sp.GetRequiredKeyedService<IDeliveryApi>(clientName);
        var contentItemMapper = sp.GetRequiredService<ContentItemMapper>();

        // Resolve keyed cache manager for this client (registered via AddDeliveryMemoryCache/AddDeliveryDistributedCache)
        var cacheManager = sp.GetKeyedService<IDeliveryCacheManager>(clientName);

        // Resolve logger (optional - will be null if no logging is configured)
        var logger = sp.GetService<ILogger<DeliveryClient>>();

        return new DeliveryClient(
            deliveryApi,
            namedMonitor,
            contentItemMapper,
            cacheManager,
            logger);
    }
}