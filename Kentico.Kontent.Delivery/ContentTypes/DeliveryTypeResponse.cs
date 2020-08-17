using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes
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
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content type.</param>
        /// <param name="type">A content type.</param>
        [JsonConstructor]
        internal DeliveryTypeResponse(ApiResponse response, IContentType type) : base(response)
        {
            Type = type;
        }
    }
}
