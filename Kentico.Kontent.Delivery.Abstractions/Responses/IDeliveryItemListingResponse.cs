using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.Models;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
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
        /// Gets the dynamic view of the JSON response where linked items and their properties can be retrieved by name, for example <c>LinkedItems.about_us.elements.description.value</c>.
        /// </summary>
        dynamic LinkedItems { get; }

        /// <summary>
        /// Gets paging information.
        /// </summary>
        IPagination Pagination { get; }
    }
}