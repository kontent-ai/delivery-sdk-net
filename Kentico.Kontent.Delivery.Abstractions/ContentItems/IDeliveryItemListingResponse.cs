using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.ContentItems
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">The type of content items in the response.</typeparam>
    public interface IDeliveryItemListingResponse<out T> : IResponse
    {
        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        IReadOnlyList<object> LinkedItems { get; }

        /// <summary>
        /// Gets paging information.
        /// </summary>
        IPagination Pagination { get; }
    }
}