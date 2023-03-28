using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kontent.ai Delivery API that contains a content item.
    /// </summary>
    public interface IDeliveryUniversalItemResponse: IDeliveryItemResponse<IUniversalContentItem>, IResponse
    {
        /// <summary>
        /// Gets the content item.
        /// </summary>
        IUniversalContentItem Item { get; }

        Dictionary<string, IUniversalContentItem> LinkedItems { get; }
    }
}