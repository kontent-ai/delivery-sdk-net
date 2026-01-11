using Microsoft.Extensions.DependencyInjection;

namespace Kontent.Ai.Delivery.Configuration;

/// <summary>
/// Implementation of <see cref="IDeliveryClientContainer"/> that owns the service provider lifetime.
/// </summary>
/// <remarks>
/// This class ensures proper disposal of the internal service provider and all registered services
/// when the container is disposed. This includes HttpClient handlers, cache managers, and other
/// disposable dependencies.
/// </remarks>
internal sealed class DeliveryClientContainer : IDeliveryClientContainer
{
    private readonly ServiceProvider _serviceProvider;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryClientContainer"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider that owns the client and its dependencies.</param>
    /// <param name="client">The delivery client instance.</param>
    internal DeliveryClientContainer(ServiceProvider serviceProvider, IDeliveryClient client)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc/>
    public IDeliveryClient Client { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _serviceProvider.Dispose();
        _disposed = true;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _serviceProvider.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }
}
