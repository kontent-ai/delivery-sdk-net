using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.SharedModels
{
    /// <summary>
    /// Represents a successful response from Kentico Kontent Delivery API.
    /// </summary>
    internal abstract class AbstractResponse : IResponse
    {
        /// <summary>
        /// The successful JSON response from Kentico Kontent Delivery API.
        /// </summary>
        public IApiResponse ApiResponse { get; set; }

        /// <summary>
        /// Default serializer.
        /// </summary>
        protected JsonSerializer Serializer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractResponse"/> class.
        /// </summary>
        /// <param name="response">A successful JSON response from Kentico Kontent Delivery API.</param>
        protected AbstractResponse(IApiResponse response)
        {
            ApiResponse = response;

            Serializer = new JsonSerializer()
            {
                ContractResolver = new DeliveryContractResolver()
            };
        }
    }
}
