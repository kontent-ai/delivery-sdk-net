namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// A container that owns the lifetime of a <see cref="IDeliveryClient"/> and its dependencies.
/// </summary>
/// <remarks>
/// <para>
/// When using DeliveryClientBuilder to create a client outside of dependency injection,
/// this container manages the lifetime of the internal service provider and all registered services
/// (HttpClient, cache managers, etc.).
/// </para>
/// <para>
/// Always dispose this container when you're done using the client to release resources properly.
/// </para>
/// <example>
/// <code>
/// using var container = DeliveryClientBuilder
///     .WithOptions(opts => opts
///         .WithEnvironmentId("your-env-id")
///         .UseProductionApi()
///         .Build())
///     .Build();
///
/// var client = container.Client;
/// var result = await client.Items&lt;Article&gt;().ExecuteAsync();
/// </code>
/// </example>
/// </remarks>
public interface IDeliveryClientContainer : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the delivery client managed by this container.
    /// </summary>
    /// <remarks>
    /// The client is valid as long as the container is not disposed.
    /// Do not cache or use this client after disposing the container.
    /// </remarks>
    IDeliveryClient Client { get; }
}
