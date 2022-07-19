using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentTypes;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
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
