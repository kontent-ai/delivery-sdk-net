namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    /// <summary>
    /// A class for providing a custom dependency.
    /// </summary>
    public interface ICustomServiceProvider
    {
        /// <summary>
        /// Gets a named service.
        /// </summary>
        /// <typeparam name="T">A type of service.</typeparam>
        /// <param name="name">A name of service.</param>
        /// <returns>The component instance that provides the service otherwise returns a null.</returns>
        T GetService<T>(string name);
    }
}
