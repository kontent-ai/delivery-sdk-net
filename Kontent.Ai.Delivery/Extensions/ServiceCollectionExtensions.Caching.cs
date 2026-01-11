using System.Text.Json;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Extensions;

public static partial class ServiceCollectionExtensions
{
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
            Options.DefaultName,
            keyPrefix: string.Empty, // No prefix for default single-client scenario
            defaultExpiration);
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
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        // Register IMemoryCache if not already registered (shared across all clients)
        services.AddMemoryCache();

        // Register keyed cache manager for this client
        services.AddKeyedSingleton<IDeliveryCacheManager>(clientName, (sp, _) =>
            new MemoryCacheManager(
                sp.GetRequiredService<IMemoryCache>(),
                keyPrefix ?? clientName,
                defaultExpiration,
                sp.GetService<ILogger<MemoryCacheManager>>()));

        // Enable dependency extraction for cache invalidation
        // Replace ensures real extractor is used regardless of registration order
        services.Replace(ServiceDescriptor.Singleton<IContentDependencyExtractor, ContentDependencyExtractor>());

        return services;
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
            Options.DefaultName,
            keyPrefix: string.Empty, // No prefix for default single-client scenario
            defaultExpiration);
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
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        // Register keyed cache manager for this client
        // Use the SDK's JsonSerializerOptions with converters for proper content item serialization
        services.AddKeyedSingleton<IDeliveryCacheManager>(clientName, (sp, _) =>
        {
            var jsonOptions = sp.GetService<JsonSerializerOptions>()
                ?? RefitSettingsProvider.CreateDefaultJsonSerializerOptions();

            return new DistributedCacheManager(
                sp.GetRequiredService<IDistributedCache>(),
                keyPrefix ?? clientName,
                defaultExpiration,
                jsonOptions,
                sp.GetService<ILogger<DistributedCacheManager>>());
        });

        // Enable dependency extraction for cache invalidation
        // Replace ensures real extractor is used regardless of registration order
        services.Replace(ServiceDescriptor.Singleton<IContentDependencyExtractor, ContentDependencyExtractor>());

        return services;
    }
}


