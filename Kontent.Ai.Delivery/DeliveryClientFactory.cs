using Microsoft.Extensions.DependencyInjection;

namespace Kontent.Ai.Delivery;

/// <summary>
/// A factory class for <see cref="IDeliveryClient"/>
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DeliveryClientFactory"/> class.
/// </remarks>
/// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance.</param>
public class DeliveryClientFactory(IServiceProvider serviceProvider) : IDeliveryClientFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private string _notImplementExceptionMessage = "The default implementation does not support retrieving clients by name. Please use the Kontent.Ai.Delivery.Extensions.Autofac.DependencyInjection or implement your own factory.";

    /// <inheritdoc />
    public IDeliveryClient Get(string name) => throw new NotImplementedException(_notImplementExceptionMessage);

    /// <inheritdoc />	
    public IDeliveryClient Get()
    {
        return _serviceProvider.GetRequiredService<IDeliveryClient>();
    }

}
