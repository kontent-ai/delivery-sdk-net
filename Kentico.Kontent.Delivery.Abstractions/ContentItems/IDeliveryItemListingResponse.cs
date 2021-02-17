using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">The type of content items in the response.</typeparam>
    public interface IDeliveryItemListingResponse<T> : IResponse, IPageable
    {
        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        IList<T> Items { get; }

        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        dynamic LinkedItems { get; }
    }
}