using System.ComponentModel.DataAnnotations;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Extension methods for registering Kontent.ai Delivery SDK caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a custom cache manager for the default Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="createCacheManager">Factory for creating the cache manager instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryCacheManager(
        this IServiceCollection services,
        Func<IServiceProvider, IDeliveryCacheManager> createCacheManager) => services.AddDeliveryCacheManager(DeliveryClientNames.Default, createCacheManager);

    /// <summary>
    /// Registers a custom cache manager for a specific named Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="createCacheManager">Factory for creating the cache manager instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliveryCacheManager(
        this IServiceCollection services,
        string clientName,
        Func<IServiceProvider, IDeliveryCacheManager> createCacheManager)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(clientName);
        ArgumentNullException.ThrowIfNull(createCacheManager);

        return RegisterCacheManager(services, clientName, createCacheManager);
    }

    /// <summary>
    /// Registers a memory cache manager for the default Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultExpiration">
    /// Default cache entry expiration time. If null, defaults to 1 hour.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience overload for single-client scenarios. It registers caching
    /// for the default (unnamed) Delivery client registered via <c>AddDeliveryClient(options => ...)</c>.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// services.AddDeliveryClient(o => o.EnvironmentId = envId);
    /// services.AddDeliveryMemoryCache(defaultExpiration: TimeSpan.FromHours(2));
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryMemoryCache(
        this IServiceCollection services,
        TimeSpan? defaultExpiration = null)
    {
        return services.AddDeliveryMemoryCache(
            DeliveryClientNames.Default,
            options =>
            {
                if (defaultExpiration.HasValue)
                {
                    options.DefaultExpiration = defaultExpiration.Value;
                }
            });
    }

    /// <summary>
    /// Registers a memory cache manager for the default Delivery client with advanced configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload to enable advanced features like fail-safe and jitter.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// services.AddDeliveryClient(o => o.EnvironmentId = envId);
    /// services.AddDeliveryMemoryCache(opts =>
    /// {
    ///     opts.DefaultExpiration = TimeSpan.FromHours(2);
    ///     opts.IsFailSafeEnabled = true;
    ///     opts.JitterMaxDuration = TimeSpan.FromSeconds(30);
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryMemoryCache(
        this IServiceCollection services,
        Action<DeliveryCacheOptions> configureCacheOptions)
    {
        return services.AddDeliveryMemoryCache(
            DeliveryClientNames.Default,
            configureCacheOptions);
    }

    /// <summary>
    /// Registers a memory cache manager for the default Delivery client with a configuration action that can resolve services from the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/> with access to the <see cref="IServiceProvider"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when the cache options need to read values from singleton-safe services registered in the
    /// container, e.g. <c>sp.GetRequiredService&lt;IOptions&lt;SiteOptions&gt;&gt;().Value</c>. The callback is invoked on first
    /// cache-manager resolution from the root provider, not at registration time.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryMemoryCache(
        this IServiceCollection services,
        Action<IServiceProvider, DeliveryCacheOptions> configureCacheOptions)
    {
        return services.AddDeliveryMemoryCache(
            DeliveryClientNames.Default,
            configureCacheOptions);
    }

    /// <summary>
    /// Registers a memory cache manager for a specific named Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="keyPrefix">
    /// Optional prefix for cache keys. If null, defaults to the client name
    /// (or empty string for the default client).
    /// Used to isolate cache entries when multiple clients share the same <see cref="IMemoryCache"/>.
    /// Set to empty string (<c>""</c>) to disable prefixing.
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
        return services.AddDeliveryMemoryCache(
            clientName,
            options =>
            {
                options.KeyPrefix = keyPrefix;
                if (defaultExpiration.HasValue)
                {
                    options.DefaultExpiration = defaultExpiration.Value;
                }
            });
    }

    /// <summary>
    /// Registers a memory cache manager for a specific named Delivery client with advanced configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload to enable advanced features like fail-safe and jitter.
    /// If <see cref="DeliveryCacheOptions.KeyPrefix"/> is not set, it defaults to the client name
    /// (or empty string for the default client).
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// services.AddDeliveryClient("production", o => o.EnvironmentId = prodEnvId);
    /// services.AddDeliveryMemoryCache("production", opts =>
    /// {
    ///     opts.DefaultExpiration = TimeSpan.FromHours(2);
    ///     opts.IsFailSafeEnabled = true;
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryMemoryCache(
        this IServiceCollection services,
        string clientName,
        Action<DeliveryCacheOptions> configureCacheOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(clientName);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        var cacheOptions = CreateCacheOptions(clientName, configureCacheOptions);

        return AddDeliveryMemoryCacheCore(services, clientName, _ => cacheOptions);
    }

    /// <summary>
    /// Registers a memory cache manager for a specific named Delivery client with a configuration action that can resolve services from the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/> with access to the <see cref="IServiceProvider"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when the cache options need to read values from singleton-safe services registered in the container,
    /// e.g. <c>sp.GetRequiredService&lt;IOptions&lt;SiteOptions&gt;&gt;().Value</c>. The callback is invoked the first
    /// time the cache manager is resolved from the root provider rather than at registration time; validation is deferred accordingly.
    /// </para>
    /// <para>
    /// If <see cref="DeliveryCacheOptions.KeyPrefix"/> is not set, it defaults to the client name
    /// (or empty string for the default client).
    /// </para>
    /// <para>
    /// <b>Avoid circular dependencies:</b> the callback must not resolve <c>IDeliveryClient</c>, <c>IDeliveryApi</c>, or any
    /// service that transitively depends on them — doing so will cause recursion when the cache manager is resolved.
    /// The callback also must not depend on scoped/request services such as <c>IOptionsSnapshot&lt;T&gt;</c>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryMemoryCache(
        this IServiceCollection services,
        string clientName,
        Action<IServiceProvider, DeliveryCacheOptions> configureCacheOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(clientName);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        return AddDeliveryMemoryCacheCore(
            services,
            clientName,
            sp => CreateCacheOptions(clientName, opts => configureCacheOptions(sp, opts)));
    }

    /// <summary>
    /// Registers a hybrid cache manager for the default Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultExpiration">
    /// Default cache entry expiration time. If null, defaults to 1 hour.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience overload for single-client scenarios. It registers caching
    /// for the default (unnamed) Delivery client registered via <c>AddDeliveryClient(options => ...)</c>.
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
    /// services.AddDeliveryClient(o => o.EnvironmentId = envId);
    /// services.AddDeliveryHybridCache(defaultExpiration: TimeSpan.FromHours(2));
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryHybridCache(
        this IServiceCollection services,
        TimeSpan? defaultExpiration = null)
    {
        return services.AddDeliveryHybridCache(
            DeliveryClientNames.Default,
            options =>
            {
                if (defaultExpiration.HasValue)
                {
                    options.DefaultExpiration = defaultExpiration.Value;
                }
            });
    }

    /// <summary>
    /// Registers a hybrid cache manager for the default Delivery client with advanced configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload to enable advanced features like fail-safe and jitter.
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> You must register an <see cref="IDistributedCache"/> implementation before calling this method.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");
    /// services.AddDeliveryClient(o => o.EnvironmentId = envId);
    /// services.AddDeliveryHybridCache(opts =>
    /// {
    ///     opts.DefaultExpiration = TimeSpan.FromHours(2);
    ///     opts.IsFailSafeEnabled = true;
    ///     opts.JitterMaxDuration = TimeSpan.FromSeconds(30);
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryHybridCache(
        this IServiceCollection services,
        Action<DeliveryCacheOptions> configureCacheOptions)
    {
        return services.AddDeliveryHybridCache(
            DeliveryClientNames.Default,
            configureCacheOptions);
    }

    /// <summary>
    /// Registers a hybrid cache manager for the default Delivery client with a configuration action that can resolve services from the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/> with access to the <see cref="IServiceProvider"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when the cache options need to read values from singleton-safe services registered in the container.
    /// The callback is invoked on first cache-manager resolution from the root provider, not at registration time.
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> You must register an <see cref="IDistributedCache"/> implementation before calling this method.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryHybridCache(
        this IServiceCollection services,
        Action<IServiceProvider, DeliveryCacheOptions> configureCacheOptions)
    {
        return services.AddDeliveryHybridCache(
            DeliveryClientNames.Default,
            configureCacheOptions);
    }

    /// <summary>
    /// Registers a hybrid cache manager for a specific named Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="keyPrefix">
    /// Optional prefix for cache keys. If null, defaults to the client name
    /// (or empty string for the default client).
    /// Used to isolate cache entries when multiple clients share the same <see cref="IDistributedCache"/>.
    /// Set to empty string (<c>""</c>) to disable prefixing.
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
    /// services.AddDeliveryHybridCache("production", defaultExpiration: TimeSpan.FromHours(2));
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryHybridCache(
        this IServiceCollection services,
        string clientName,
        string? keyPrefix = null,
        TimeSpan? defaultExpiration = null)
    {
        return services.AddDeliveryHybridCache(
            clientName,
            options =>
            {
                options.KeyPrefix = keyPrefix;
                if (defaultExpiration.HasValue)
                {
                    options.DefaultExpiration = defaultExpiration.Value;
                }
            });
    }

    /// <summary>
    /// Registers a hybrid cache manager for a specific named Delivery client with advanced configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload to enable advanced features like fail-safe and jitter.
    /// If <see cref="DeliveryCacheOptions.KeyPrefix"/> is not set, it defaults to the client name
    /// (or empty string for the default client).
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> You must register an <see cref="IDistributedCache"/> implementation before calling this method.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");
    /// services.AddDeliveryClient("production", o => o.EnvironmentId = prodEnvId);
    /// services.AddDeliveryHybridCache("production", opts =>
    /// {
    ///     opts.DefaultExpiration = TimeSpan.FromHours(2);
    ///     opts.IsFailSafeEnabled = true;
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryHybridCache(
        this IServiceCollection services,
        string clientName,
        Action<DeliveryCacheOptions> configureCacheOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(clientName);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        var cacheOptions = CreateCacheOptions(clientName, configureCacheOptions);

        return AddDeliveryHybridCacheCore(services, clientName, _ => cacheOptions);
    }

    /// <summary>
    /// Registers a hybrid cache manager for a specific named Delivery client with a configuration action that can resolve services from the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/> with access to the <see cref="IServiceProvider"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when the cache options need to read values from singleton-safe services registered in the container.
    /// The callback is invoked the first time the cache manager is resolved from the root provider rather than at registration time; validation is deferred accordingly.
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> You must register an <see cref="IDistributedCache"/> implementation before calling this method.
    /// </para>
    /// <para>
    /// If <see cref="DeliveryCacheOptions.KeyPrefix"/> is not set, it defaults to the client name
    /// (or empty string for the default client).
    /// </para>
    /// <para>
    /// <b>Avoid circular dependencies:</b> the callback must not resolve <c>IDeliveryClient</c>, <c>IDeliveryApi</c>, or any
    /// service that transitively depends on them — doing so will cause recursion when the cache manager is resolved.
    /// The callback also must not depend on scoped/request services such as <c>IOptionsSnapshot&lt;T&gt;</c>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryHybridCache(
        this IServiceCollection services,
        string clientName,
        Action<IServiceProvider, DeliveryCacheOptions> configureCacheOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(clientName);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        return AddDeliveryHybridCacheCore(
            services,
            clientName,
            sp => CreateCacheOptions(clientName, opts => configureCacheOptions(sp, opts)));
    }

    private static IServiceCollection AddDeliveryMemoryCacheCore(
        IServiceCollection services,
        string clientName,
        Func<IServiceProvider, DeliveryCacheOptions> cacheOptionsFactory)
    {
        // Register IMemoryCache if not already registered (shared across all clients)
        services.AddMemoryCache();

        return RegisterCacheManager(
            services,
            clientName,
            sp => new MemoryCacheManager(
                sp.GetRequiredService<IMemoryCache>(),
                cacheOptionsFactory(sp),
                sp.GetService<ILogger<MemoryCacheManager>>()));
    }

    private static IServiceCollection AddDeliveryHybridCacheCore(
        IServiceCollection services,
        string clientName,
        Func<IServiceProvider, DeliveryCacheOptions> cacheOptionsFactory)
    {
        return RegisterCacheManager(
            services,
            clientName,
            sp => new HybridCacheManager(
                sp.GetRequiredService<IDistributedCache>(),
                cacheOptionsFactory(sp),
                logger: sp.GetService<ILogger<HybridCacheManager>>()));
    }

    private static IServiceCollection RegisterCacheManager(
        IServiceCollection services,
        string clientName,
        Func<IServiceProvider, IDeliveryCacheManager> createCacheManager)
    {
        RemoveExistingCacheManagerRegistration(services, clientName);
        services.AddKeyedSingleton<IDeliveryCacheManager>(clientName, (sp, _) => createCacheManager(sp));
        services.Replace(ServiceDescriptor.Singleton<IContentDependencyExtractor, ContentDependencyExtractor>());
        return services;
    }

    private static void RemoveExistingCacheManagerRegistration(IServiceCollection services, string clientName)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            var descriptor = services[i];
            if (descriptor.ServiceType == typeof(IDeliveryCacheManager) &&
                Equals(descriptor.ServiceKey, clientName))
            {
                services.RemoveAt(i);
            }
        }
    }

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

    private static DeliveryCacheOptions CreateCacheOptions(
        string clientName,
        Action<DeliveryCacheOptions> configureCacheOptions)
    {
        var cacheOptions = new DeliveryCacheOptions();
        configureCacheOptions(cacheOptions);
        cacheOptions.KeyPrefix = ResolveCacheKeyPrefix(clientName, cacheOptions.KeyPrefix);

        return ValidateCacheOptions(cacheOptions);
    }

    private static DeliveryCacheOptions ValidateCacheOptions(DeliveryCacheOptions cacheOptions)
    {
        ArgumentNullException.ThrowIfNull(cacheOptions);
        Validator.ValidateObject(cacheOptions, new ValidationContext(cacheOptions), validateAllProperties: true);

        return cacheOptions;
    }

    private static string ResolveCacheKeyPrefix(string clientName, string? keyPrefix)
    {
        if (keyPrefix is not null)
        {
            return keyPrefix;
        }

        return clientName == DeliveryClientNames.Default ? string.Empty : clientName;
    }

}
