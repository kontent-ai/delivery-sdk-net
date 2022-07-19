using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.TaxonomyGroups
{
    /// <inheritdoc cref="IDeliveryTaxonomyResponse" />
    internal sealed class DeliveryTaxonomyResponse : AbstractResponse, IDeliveryTaxonomyResponse
    {
        /// <inheritdoc/>
        public ITaxonomyGroup Taxonomy
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains a taxonomy group.</param>
        /// <param name="taxonomy">A taxonomy group.</param>
        [JsonConstructor]
        internal DeliveryTaxonomyResponse(ApiResponse response, ITaxonomyGroup taxonomy) : base(response)
        {
            Taxonomy = taxonomy;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains a taxonomy group.</param>
        internal DeliveryTaxonomyResponse(ApiResponse response) : base(response)
        {
        }
    }
}
