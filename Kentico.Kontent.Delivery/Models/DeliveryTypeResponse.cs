using Kentico.Kontent.Delivery.Abstractions.Models.Type;
using Kentico.Kontent.Delivery.Abstractions.Responses;
using Kentico.Kontent.Delivery.Models.Type;
using System;
using System.Threading;

namespace Kentico.Kontent.Delivery.Models
{
    /// <inheritdoc/>
    public sealed class DeliveryTypeResponse : AbstractResponse, IDeliveryTypeResponse
    {
        private readonly Lazy<ContentType> _type;

        /// <inheritdoc/>
        public IContentType Type => _type.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content type.</param>
        internal DeliveryTypeResponse(ApiResponse response) : base(response)
        {
            _type = new Lazy<ContentType>(() => new ContentType(response.JsonContent), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
