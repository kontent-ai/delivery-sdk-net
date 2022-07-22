using System.Diagnostics;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentTypes
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
        /// <param name="response">The response from Kontent.ai Delivery API that contains a content element.</param>
        /// <param name="element">A content element.</param>
        [JsonConstructor]
        internal DeliveryElementResponse(ApiResponse response, IContentElement element) : base(response)
        {
            Element = element;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryElementResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent.ai Delivery API that contains a content element.</param>
        internal DeliveryElementResponse(ApiResponse response) : base(response)
        {
        }
    }
}
