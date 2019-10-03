using System;
using System.Threading;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a content type element.
    /// </summary>
    public sealed class DeliveryElementResponse : AbstractResponse
    {
        private readonly Lazy<ContentElement> _element;

        /// <summary>
        /// Gets the content type element.
        /// </summary>
        public ContentElement Element => _element.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryElementResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content type element.</param>
        internal DeliveryElementResponse(ApiResponse response) : base(response)
        {
            _element = new Lazy<ContentElement>(() => new ContentElement(_response.Content, _response.Content.Value<string>("codename")), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Implicitly converts the specified <paramref name="response"/> to a content type element.
        /// </summary>
        /// <param name="response">The response to convert.</param>
        public static implicit operator ContentElement(DeliveryElementResponse response) => response.Element;
    }
}
