namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// A factory class for <see cref="IDeliveryClient"/>
    /// </summary>
    public interface IDeliveryClientFactory
    {
        /// <summary>
        /// Returns a named instance of the <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <param name="name">A name of the configuration to be used to instantiate the client.</param>
        /// <returns>Returns an <see cref="IDeliveryClient"/> instance with the given name.</returns>
        IDeliveryClient Get(string name);

        /// <summary>	
        /// Returns a default instance of the <see cref="IDeliveryClient"/>.	
        /// </summary>	
        /// <returns>Returns a default instance of the <see cref="IDeliveryClient"/>.</returns>	
        IDeliveryClient Get();
    }
}
