using Kentico.Kontent.Delivery.Abstractions.Models.Type.Element;
using Kentico.Kontent.Delivery.Abstractions.Responses;
using Kentico.Kontent.Delivery.Models.Type.Element;
using System;
using System.Threading;

namespace Kentico.Kontent.Delivery.Models
{
    /// <inheritdoc/>
    public sealed class DeliveryElementResponse : AbstractResponse, IDeliveryElementResponse
    {
        private readonly Lazy<ContentElement> _element;

        /// <inheritdoc/>
        public IContentElement Element => _element.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryElementResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content type element.</param>
        internal DeliveryElementResponse(ApiResponse response) : base(response)
        {
            _element = new Lazy<ContentElement>(() => new ContentElement(response.JsonContent, response.JsonContent.Value<string>("codename")), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
