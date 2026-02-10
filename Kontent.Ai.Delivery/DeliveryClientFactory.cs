using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kontent.Ai.Delivery;

/// <summary>
/// A factory class for <see cref="IDeliveryClient"/>.
/// Supports both default (unnamed) and named client retrieval.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DeliveryClientFactory"/> class.
/// </remarks>
/// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance.</param>
public sealed class DeliveryClientFactory(IServiceProvider serviceProvider) : IDeliveryClientFactory
{
    /// <inheritdoc />
    public IDeliveryClient Get() => Get(DeliveryClientNames.Default);

    /// <inheritdoc />
    public IDeliveryClient Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return serviceProvider.GetRequiredKeyedService<IDeliveryClient>(name);
    }
}
