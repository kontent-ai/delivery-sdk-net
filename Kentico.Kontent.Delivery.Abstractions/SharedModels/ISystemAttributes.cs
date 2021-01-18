using System;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents extended system attributes of any object in Kentico Kontent.
    /// </summary>
    public interface ISystemAttributes : ISystemBaseAttributes
    {
        /// <summary>
        /// Gets the time the object was last modified.
        /// </summary>
        DateTime LastModified { get; }
    }
}
