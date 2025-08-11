using Kontent.Ai.Delivery.SharedModels;
using IApiResponse = Kontent.Ai.Delivery.Abstractions.IApiResponse; // TODO: Remove this once we adopt ApiResponse from Refit

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
        /// <param name="response">The response from Kontent.ai Delivery API that contains a content item.</param>
        protected AbstractItemsResponse(IApiResponse response) : base(response) { }
    }
}
