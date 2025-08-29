using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.SharedModels;
using System.Text.Json.Serialization;
using IApiResponse = Kontent.Ai.Delivery.Abstractions.IApiResponse;

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
        /// <param name="response">The response from Kontent.ai Delivery API that contains a taxonomy group.</param>
        /// <param name="taxonomy">A taxonomy group.</param>
        [JsonConstructor]
        internal DeliveryTaxonomyResponse(IApiResponse response, ITaxonomyGroup taxonomy) : base(response)
        {
            Taxonomy = taxonomy;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent.ai Delivery API that contains a taxonomy group.</param>
        internal DeliveryTaxonomyResponse(IApiResponse response) : base(response)
        {
        }
    }
}
