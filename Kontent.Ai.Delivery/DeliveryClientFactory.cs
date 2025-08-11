namespace Kontent.Ai.Delivery
{
    /// <summary>
    /// A factory class for <see cref="IDeliveryClient"/>
    /// </summary>
    public class DeliveryClientFactory : IDeliveryClientFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private string _notImplementExceptionMessage = "The default implementation does not support retrieving clients by name. Please use the Kontent.Ai.Delivery.Extensions.Autofac.DependencyInjection or implement your own factory.";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClientFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance.</param>
        public DeliveryClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public IDeliveryClient Get(string name) => throw new NotImplementedException(_notImplementExceptionMessage);

        /// <inheritdoc />	
        public IDeliveryClient Get()
        {
            return _serviceProvider.GetRequiredService<IDeliveryClient>();
        }

    }
}
