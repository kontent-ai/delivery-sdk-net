using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.ContentTypes
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of content types.
    /// </summary>
    public interface IDeliveryTypeListingResponse : IResponse
    {
        /// <summary>
        /// Gets paging information.
        /// </summary>
        IPagination Pagination { get; }

        /// <summary>
        /// Gets a read-only list of content types.
        /// </summary>
        IReadOnlyList<IContentType> Types { get; }
    }
}