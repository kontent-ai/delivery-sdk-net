using System.Collections.Generic;
using Kentico.Kontent.Delivery.SharedModels;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <summary>
    /// Response object with built-in linked items resolution.
    /// </summary>
    internal abstract class AbstractItemsResponse : AbstractResponse
    {
        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        public IList<object> LinkedItems
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractItemsResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        /// <param name="linkedItems">Collection of linked content items.</param>
        protected AbstractItemsResponse(ApiResponse response, IList<object> linkedItems) : base(response)
        {
            LinkedItems = linkedItems;
        }
    }
}
