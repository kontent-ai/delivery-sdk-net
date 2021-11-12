using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.ContentItems
{
    /// <summary>
    /// Represents a partial response from Kontent Delivery API enumeration methods that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">The type of content items in the response.</typeparam>
    public interface IDeliveryItemsFeedResponse<T> : IResponse
    {
        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        IList<T> Items { get; }
    }
}