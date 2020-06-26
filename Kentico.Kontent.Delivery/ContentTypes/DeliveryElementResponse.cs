using System;
using System.Diagnostics;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentTypes.Element;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes
{
    /// <inheritdoc cref="IDeliveryElementResponse" />
    [DebuggerDisplay("Name = {" + nameof(Element) + "." + nameof(IContentElement.Name) + "}")]
    internal sealed class DeliveryElementResponse : AbstractResponse, IDeliveryElementResponse
    {
        private Lazy<IContentElement> _element;

        /// <inheritdoc/>
        public IContentElement Element
        {
            get => _element.Value;
            private set => _element = new Lazy<IContentElement>(() => value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryElementResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content type element.</param>
        internal DeliveryElementResponse(ApiResponse response) : base(response)
        {
            _element = new Lazy<IContentElement>(() => new ContentElement(response.JsonContent, response.JsonContent.Value<string>("codename")), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains content elements.</param>
        /// <param name="element">A content element.</param>
        [JsonConstructor]
        internal DeliveryElementResponse(ApiResponse response, IContentElement element) : base(response)
        {
            Element = element;
        }
    }
}
