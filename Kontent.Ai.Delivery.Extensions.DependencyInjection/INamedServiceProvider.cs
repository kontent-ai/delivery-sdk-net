using System;

namespace Kontent.Ai.Delivery.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents a contract for retrieving named dependencies.
    /// </summary>
    [Obsolete("#312")]
    public interface INamedServiceProvider
    {
        /// <summary>
        /// Gets a named service.
        /// </summary>
        /// <typeparam name="T">A type of service.</typeparam>
        /// <param name="name">A name of service.</param>
        /// <returns>The component instance that provides the service otherwise returns null.</returns>
        T GetService<T>(string name);
    }
}
