using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Extension methods for configuring caching on <see cref="DeliveryClientBuilder"/>.
/// </summary>
public static class DeliveryClientBuilderExtensions
{
    /// <summary>
    /// Enables in-memory caching for API responses.
    /// </summary>
    /// <param name="builder">The delivery client builder.</param>
    /// <param name="defaultExpiration">
    /// Default cache entry expiration time. If null, defaults to 1 hour.
    /// Individual queries can override this using query parameters.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The builder creates and manages an <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> instance internally.
    /// For scenarios requiring shared cache instances across multiple clients, use DI registration instead.
    /// </para>
    /// <para>
    /// Cannot be combined with hybrid cache. Calling both will use the last one configured.
    /// </para>
    /// </remarks>
    public static DeliveryClientBuilder WithMemoryCache(this DeliveryClientBuilder builder, TimeSpan? defaultExpiration = null) => builder.ConfigureServices(services => services.AddDeliveryMemoryCache(defaultExpiration));

    /// <summary>
    /// Enables in-memory caching for API responses with advanced configuration.
    /// </summary>
    /// <param name="builder">The delivery client builder.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/>.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configureCacheOptions"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Use this overload to enable advanced features like fail-safe and jitter:
    /// <code>
    /// .WithMemoryCache(opts =>
    /// {
    ///     opts.DefaultExpiration = TimeSpan.FromMinutes(30);
    ///     opts.IsFailSafeEnabled = true;
    ///     opts.JitterMaxDuration = TimeSpan.FromSeconds(30);
    /// })
    /// </code>
    /// </para>
    /// <para>
    /// Cannot be combined with hybrid cache. Calling both will use the last one configured.
    /// </para>
    /// </remarks>
    public static DeliveryClientBuilder WithMemoryCache(this DeliveryClientBuilder builder, Action<DeliveryCacheOptions> configureCacheOptions)
    {
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        return builder.ConfigureServices(services => services.AddDeliveryMemoryCache(configureCacheOptions));
    }

    /// <summary>
    /// Enables in-memory caching for API responses with a configuration action that can resolve services from the container.
    /// </summary>
    /// <param name="builder">The delivery client builder.</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/> with access to the <see cref="IServiceProvider"/>.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configureCacheOptions"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The callback receives the builder's <b>internal</b> <see cref="IServiceProvider"/> — not your application's DI container.
    /// To make sibling services available to the callback, register them via <see cref="DeliveryClientBuilder.ConfigureServices"/>.
    /// The callback is invoked on first cache-manager resolution from the builder's root provider.
    /// </para>
    /// <para>
    /// Cannot be combined with hybrid cache. Calling both will use the last one configured.
    /// </para>
    /// </remarks>
    public static DeliveryClientBuilder WithMemoryCache(this DeliveryClientBuilder builder, Action<IServiceProvider, DeliveryCacheOptions> configureCacheOptions)
    {
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        return builder.ConfigureServices(services => services.AddDeliveryMemoryCache(configureCacheOptions));
    }

    /// <summary>
    /// Enables hybrid (L1 memory + L2 distributed) caching for API responses using a provided <see cref="IDistributedCache"/> instance.
    /// </summary>
    /// <param name="builder">The delivery client builder.</param>
    /// <param name="distributedCache">
    /// The distributed cache instance (e.g., Redis, SQL Server, NCache).
    /// </param>
    /// <param name="defaultExpiration">
    /// Default cache entry expiration time. If null, defaults to 1 hour.
    /// Individual queries can override this using query parameters.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="distributedCache"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Unlike the memory cache overloads, this method requires you to provide the distributed cache instance.
    /// This is because distributed cache implementations (Redis, SQL Server, etc.) require external configuration.
    /// </para>
    /// <para>
    /// Cannot be combined with memory cache. Calling both will use the last one configured.
    /// </para>
    /// </remarks>
    public static DeliveryClientBuilder WithHybridCache(this DeliveryClientBuilder builder, IDistributedCache distributedCache, TimeSpan? defaultExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(distributedCache);

        return builder.ConfigureServices(services =>
        {
            services.AddSingleton(distributedCache);
            services.AddDeliveryHybridCache(defaultExpiration);
        });
    }

    /// <summary>
    /// Enables hybrid (L1 memory + L2 distributed) caching for API responses with advanced configuration.
    /// </summary>
    /// <param name="builder">The delivery client builder.</param>
    /// <param name="distributedCache">
    /// The distributed cache instance (e.g., Redis, SQL Server, NCache).
    /// </param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/>.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="distributedCache"/> or <paramref name="configureCacheOptions"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Use this overload to enable advanced features like fail-safe and jitter:
    /// <code>
    /// .WithHybridCache(redisCache, opts =>
    /// {
    ///     opts.DefaultExpiration = TimeSpan.FromMinutes(30);
    ///     opts.IsFailSafeEnabled = true;
    ///     opts.JitterMaxDuration = TimeSpan.FromSeconds(30);
    /// })
    /// </code>
    /// </para>
    /// <para>
    /// Cannot be combined with memory cache. Calling both will use the last one configured.
    /// </para>
    /// </remarks>
    public static DeliveryClientBuilder WithHybridCache(this DeliveryClientBuilder builder, IDistributedCache distributedCache, Action<DeliveryCacheOptions> configureCacheOptions)
    {
        ArgumentNullException.ThrowIfNull(distributedCache);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        return builder.ConfigureServices(services =>
        {
            services.AddSingleton(distributedCache);
            services.AddDeliveryHybridCache(configureCacheOptions);
        });
    }

    /// <summary>
    /// Enables hybrid (L1 memory + L2 distributed) caching with a configuration action that can resolve services from the container.
    /// </summary>
    /// <param name="builder">The delivery client builder.</param>
    /// <param name="distributedCache">The distributed cache instance (e.g., Redis, SQL Server, NCache).</param>
    /// <param name="configureCacheOptions">A delegate to configure the <see cref="DeliveryCacheOptions"/> with access to the <see cref="IServiceProvider"/>.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="distributedCache"/> or <paramref name="configureCacheOptions"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The callback receives the builder's <b>internal</b> <see cref="IServiceProvider"/> — not your application's DI container.
    /// To make sibling services available to the callback, register them via <see cref="DeliveryClientBuilder.ConfigureServices"/>.
    /// The callback is invoked on first cache-manager resolution from the builder's root provider.
    /// </para>
    /// <para>
    /// Cannot be combined with memory cache. Calling both will use the last one configured.
    /// </para>
    /// </remarks>
    public static DeliveryClientBuilder WithHybridCache(this DeliveryClientBuilder builder, IDistributedCache distributedCache, Action<IServiceProvider, DeliveryCacheOptions> configureCacheOptions)
    {
        ArgumentNullException.ThrowIfNull(distributedCache);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);

        return builder.ConfigureServices(services =>
        {
            services.AddSingleton(distributedCache);
            services.AddDeliveryHybridCache(configureCacheOptions);
        });
    }
}
