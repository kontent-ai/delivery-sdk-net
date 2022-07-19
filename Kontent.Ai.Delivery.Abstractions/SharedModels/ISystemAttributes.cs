using System;

namespace Kontent.Ai.Delivery.Abstractions
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
