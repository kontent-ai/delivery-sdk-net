using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.ContentTypes
{
    /// <summary>
    /// Represents a response from Kontent Delivery API that contains a list of content types.
    /// </summary>
    public interface IDeliveryTypeListingResponse : IResponse, IPageable
    {
        /// <summary>
        /// Gets a read-only list of content types.
        /// </summary>
        IList<IContentType> Types { get; }
    }
}