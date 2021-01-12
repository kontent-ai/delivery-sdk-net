using System;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents system attributes of ContentItem, ContentType and Taxonomy objects in Kentico Kontent.
    /// </summary>
    public interface ISystemAttributes : ISystemBaseAttributes
    {
        /// <summary>
        /// Gets the time the object was last modified.
        /// </summary>
        DateTime LastModified { get; }
    }
}
