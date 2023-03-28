using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions
{
    public interface IDeliveryUniversalItemListingResponse : IResponse, IPageable
    {
        /// <summary>
        /// Gets the content item.
        /// </summary>
        // TODO why it is IList
        IList<IUniversalContentItem> Items { get; }


        Dictionary<string, IUniversalContentItem> LinkedItems { get; }
    }
}