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
            string.Empty, // No prefix for default single-client scenario
            defaultExpiration);
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
    /// Registers a memory cache manager for a specific named Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="keyPrefix">
    /// Optional prefix for cache keys. If null, defaults to the client name.
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
        var resolvedKeyPrefix = ResolveCacheKeyPrefix(clientName, keyPrefix);
        var cacheOptions = new DeliveryCacheOptions { KeyPrefix = resolvedKeyPrefix };
        if (defaultExpiration.HasValue)
        {
            cacheOptions.DefaultExpiration = defaultExpiration.Value;
        }

        return AddDeliveryMemoryCacheCore(services, clientName, cacheOptions);
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
    /// If <see cref="DeliveryCacheOptions.KeyPrefix"/> is not set, it defaults to the client name.
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
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        var cacheOptions = new DeliveryCacheOptions();
        configureCacheOptions(cacheOptions);
        cacheOptions.KeyPrefix ??= clientName;

        return AddDeliveryMemoryCacheCore(services, clientName, cacheOptions);
    }

    /// <summary>
    /// Registers a distributed cache manager for the default Delivery client.
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
    /// services.AddDeliveryDistributedCache(defaultExpiration: TimeSpan.FromHours(2));
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryDistributedCache(
        this IServiceCollection services,
        TimeSpan? defaultExpiration = null)
    {
        return services.AddDeliveryDistributedCache(
            DeliveryClientNames.Default,
            string.Empty, // No prefix for default single-client scenario
            defaultExpiration);
    }

    /// <summary>
    /// Registers a distributed cache manager for the default Delivery client with advanced configuration.
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
    /// services.AddDeliveryDistributedCache(opts =>
    /// {
    ///     opts.DefaultExpiration = TimeSpan.FromHours(2);
    ///     opts.IsFailSafeEnabled = true;
    ///     opts.JitterMaxDuration = TimeSpan.FromSeconds(30);
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryDistributedCache(
        this IServiceCollection services,
        Action<DeliveryCacheOptions> configureCacheOptions)
    {
        return services.AddDeliveryDistributedCache(
            DeliveryClientNames.Default,
            configureCacheOptions);
    }

    /// <summary>
    /// Registers a distributed cache manager for a specific named Delivery client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="keyPrefix">
    /// Optional prefix for cache keys. If null, defaults to the client name.
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
        var resolvedKeyPrefix = ResolveCacheKeyPrefix(clientName, keyPrefix);
        var cacheOptions = new DeliveryCacheOptions { KeyPrefix = resolvedKeyPrefix };
        if (defaultExpiration.HasValue)
        {
            cacheOptions.DefaultExpiration = defaultExpiration.Value;
        }

        return AddDeliveryDistributedCacheCore(services, clientName, cacheOptions);
    }

    /// <summary>
    /// Registers a distributed cache manager for a specific named Delivery client with advanced configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the Delivery client to enable caching for.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload to enable advanced features like fail-safe and jitter.
    /// If <see cref="DeliveryCacheOptions.KeyPrefix"/> is not set, it defaults to the client name.
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> You must register an <see cref="IDistributedCache"/> implementation before calling this method.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");
    /// services.AddDeliveryClient("production", o => o.EnvironmentId = prodEnvId);
    /// services.AddDeliveryDistributedCache("production", opts =>
    /// {
    ///     opts.DefaultExpiration = TimeSpan.FromHours(2);
    ///     opts.IsFailSafeEnabled = true;
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDeliveryDistributedCache(
        this IServiceCollection services,
        string clientName,
        Action<DeliveryCacheOptions> configureCacheOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(clientName);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        var cacheOptions = new DeliveryCacheOptions();
        configureCacheOptions(cacheOptions);
        cacheOptions.KeyPrefix ??= clientName;

        return AddDeliveryDistributedCacheCore(services, clientName, cacheOptions);
    }

    private static IServiceCollection AddDeliveryMemoryCacheCore(
        IServiceCollection services,
        string clientName,
        DeliveryCacheOptions cacheOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(clientName);

        // Register IMemoryCache if not already registered (shared across all clients)
        services.AddMemoryCache();

        return RegisterCacheManager(
            services,
            clientName,
            sp => new MemoryCacheManager(
                sp.GetRequiredService<IMemoryCache>(),
                cacheOptions,
                sp.GetService<ILogger<MemoryCacheManager>>()));
    }

    private static IServiceCollection AddDeliveryDistributedCacheCore(
        IServiceCollection services,
        string clientName,
        DeliveryCacheOptions cacheOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateClientName(clientName);

        return RegisterCacheManager(
            services,
            clientName,
            sp => new DistributedCacheManager(
                sp.GetRequiredService<IDistributedCache>(),
                cacheOptions,
                logger: sp.GetService<ILogger<DistributedCacheManager>>()));
    }

    private static IServiceCollection RegisterCacheManager(
        IServiceCollection services,
        string clientName,
        Func<IServiceProvider, IDeliveryCacheManager> createCacheManager)
    {
        services.AddKeyedSingleton<IDeliveryCacheManager>(clientName, (sp, _) => createCacheManager(sp));
        services.AddKeyedSingleton<IDeliveryCachePurger>(clientName, (sp, _) =>
        {
            var cacheManager = sp.GetRequiredKeyedService<IDeliveryCacheManager>(clientName);
            return cacheManager as IDeliveryCachePurger
                ?? throw new InvalidOperationException(
                    $"The cache manager registered for client '{clientName}' ({cacheManager.GetType().Name}) " +
                    $"does not implement {nameof(IDeliveryCachePurger)}.");
        });
        services.Replace(ServiceDescriptor.Singleton<IContentDependencyExtractor, ContentDependencyExtractor>());
        return services;
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

    private static string ResolveCacheKeyPrefix(string clientName, string? keyPrefix)
        => keyPrefix ?? clientName;
}
