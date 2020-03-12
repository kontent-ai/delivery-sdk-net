namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Defines a methods for getting a <see cref="IDeliveryClient"/>
    /// </summary>
    public interface IDeliveryClientFactory
    {
        /// <summary>
        /// Returns a named <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The <see cref="IDeliveryClient"/> instance that represents named client</returns>
        IDeliveryClient Get(string name);

        /// <summary>
        /// Returns an <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <returns>The <see cref="IDeliveryClient"/> instance that represents client</returns>
        IDeliveryClient Get();
    }
}
