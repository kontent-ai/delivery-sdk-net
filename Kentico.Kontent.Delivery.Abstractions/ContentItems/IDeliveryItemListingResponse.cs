using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">The type of content items in the response.</typeparam>
    public interface IDeliveryItemListingResponse<out T> : IResponse, IPageable
    {
        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        IReadOnlyList<object> LinkedItems { get; }
    }
}