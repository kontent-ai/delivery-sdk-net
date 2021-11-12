using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.SharedModels
{
    /// <summary>
    /// Represents a successful response from Kontent Delivery API.
    /// </summary>
    internal abstract class AbstractResponse : IResponse
    {
        /// <summary>
        /// The successful JSON response from Kontent Delivery API.
        /// </summary>
        public IApiResponse ApiResponse { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractResponse"/> class.
        /// </summary>
        /// <param name="response">A successful JSON response from Kontent Delivery API.</param>
        protected AbstractResponse(IApiResponse response)
        {
            ApiResponse = response;
        }
    }
}
