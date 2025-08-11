using IApiResponse = Kontent.Ai.Delivery.Abstractions.IApiResponse; // TODO: Remove this once we adopt ApiResponse from Refit

namespace Kontent.Ai.Delivery.SharedModels
{
    /// <summary>
    /// Represents a successful response from Kontent.ai Delivery API.
    /// </summary>
    internal abstract class AbstractResponse : IResponse
    {
        /// <summary>
        /// The successful JSON response from Kontent.ai Delivery API.
        /// </summary>
        public IApiResponse ApiResponse { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractResponse"/> class.
        /// </summary>
        /// <param name="response">A successful JSON response from Kontent.ai Delivery API.</param>
        protected AbstractResponse(IApiResponse response)
        {
            ApiResponse = response;
        }
    }
}
