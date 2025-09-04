using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a partial response from Kontent.ai Delivery API enumeration methods that contains a list of content items.
    /// </summary>
    /// <typeparam name="TModel">The type of content items in the response.</typeparam>
    public interface IDeliveryItemsFeedResponse<out TModel>
        where TModel : IElementsModel
    {
        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        IReadOnlyList<IContentItem<TModel>> Items { get; }
    }
}