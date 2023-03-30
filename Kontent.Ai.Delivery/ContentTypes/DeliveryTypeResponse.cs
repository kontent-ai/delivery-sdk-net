using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentTypes
{
    /// <inheritdoc cref="IDeliveryTypeResponse" />
    internal sealed class DeliveryTypeResponse : AbstractResponse, IDeliveryTypeResponse
    {
        /// <inheritdoc/>
        public IContentType Type
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent.ai Delivery API that contains a content type.</param>
        /// <param name="type">A content type.</param>
        [JsonConstructor]
        internal DeliveryTypeResponse(ApiResponse response, IContentType type) : base(response)
        {
            Type = type;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent.ai Delivery API that contains a content type.</param>
        internal DeliveryTypeResponse(ApiResponse response) : base(response)
        {
        }
    }
}
