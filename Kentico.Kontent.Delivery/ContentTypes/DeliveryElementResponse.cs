using System;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentTypes.Element;
using Kentico.Kontent.Delivery.SharedModels;

namespace Kentico.Kontent.Delivery.ContentTypes
{
    /// <inheritdoc cref="IDeliveryElementResponse" />
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
