using System;

namespace Kentico.Kontent.Delivery.Abstractions.SharedModels
{
    /// <summary>
    /// Represents extended system attributes of any object in Kontent.
    /// </summary>
    public interface ISystemAttributes : ISystemBaseAttributes
    {
        /// <summary>
        /// Gets the time the object was last modified.
        /// </summary>
        DateTime LastModified { get; }
    }
}
