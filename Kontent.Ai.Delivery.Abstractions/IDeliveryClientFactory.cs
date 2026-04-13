namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// A factory class for <see cref="IDeliveryClient"/>
/// </summary>
public interface IDeliveryClientFactory
{
    /// <summary>
    /// Returns a named instance of the <see cref="IDeliveryClient"/>.
    /// Throws if no client with the given name has been registered.
    /// </summary>
    /// <param name="name">A name of the configuration to be used to instantiate the client.</param>
    /// <returns>Returns an <see cref="IDeliveryClient"/> instance with the given name.</returns>
    IDeliveryClient Get(string name);

    /// <summary>
    /// Returns a default instance of the <see cref="IDeliveryClient"/>.
    /// Throws if no default client has been registered.
    /// </summary>
    /// <returns>Returns a default instance of the <see cref="IDeliveryClient"/>.</returns>
    IDeliveryClient Get();

    /// <summary>
    /// Returns a named instance of the <see cref="IDeliveryClient"/>, or <c>null</c> if no client
    /// with the given name has been registered.
    /// </summary>
    /// <param name="name">A name of the configuration to be used to instantiate the client.</param>
    /// <returns>
    /// An <see cref="IDeliveryClient"/> instance with the given name, or <c>null</c> if not registered.
    /// </returns>
    IDeliveryClient? TryGet(string name);
}
