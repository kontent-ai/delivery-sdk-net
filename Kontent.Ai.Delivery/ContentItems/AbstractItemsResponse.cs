using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <summary>
    /// Response object with built-in linked items resolution.
    /// </summary>
    internal abstract class AbstractItemsResponse : AbstractResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractItemsResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains a content item.</param>
        protected AbstractItemsResponse(IApiResponse response) : base(response) { }
    }
}
