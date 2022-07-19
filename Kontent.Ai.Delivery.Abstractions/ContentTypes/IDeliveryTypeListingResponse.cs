using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
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