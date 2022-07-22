using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kontent.ai Delivery API that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">The type of content items in the response.</typeparam>
    public interface IDeliveryItemListingResponse<T> : IResponse, IPageable
    {
        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        IList<T> Items { get; }
    }
}