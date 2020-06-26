using System;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes
{
    /// <inheritdoc cref="IDeliveryTypeResponse" />
    internal sealed class DeliveryTypeResponse : AbstractResponse, IDeliveryTypeResponse
    {
        private Lazy<IContentType> _type;

        /// <inheritdoc/>
        public IContentType Type
        {
            get => _type.Value;
            private set => _type = new Lazy<IContentType>(() => value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content type.</param>
        internal DeliveryTypeResponse(ApiResponse response) : base(response)
        {
            _type = new Lazy<IContentType>(() => response.JsonContent.ToObject<ContentType>(Serializer), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains content types.</param>
        /// <param name="type">A content type.</param>
        [JsonConstructor]
        internal DeliveryTypeResponse(ApiResponse response, IContentType type) : base(response)
        {
            Type = type;
        }
    }
}
