using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes
{
    /// <inheritdoc cref="IDeliveryElementResponse" />
    [DebuggerDisplay("Name = {" + nameof(Element) + "." + nameof(IContentElement.Name) + "}")]
    internal sealed class DeliveryElementResponse : AbstractResponse, IDeliveryElementResponse
    {
        /// <inheritdoc/>
        public IContentElement Element
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryElementResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content element.</param>
        /// <param name="element">A content element.</param>
        [JsonConstructor]
        internal DeliveryElementResponse(ApiResponse response, IContentElement element) : base(response)
        {
            Element = element;
        }
    }
}
