using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a partial response from Kentico Kontent Delivery API enumeration methods that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">The type of content items in the response.</typeparam>
    public interface IDeliveryItemsFeedResponse<T> : IResponse
    {
        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        IList<T> Items { get; }

        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        IList<object> LinkedItems { get; }
    }
}