using System;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentTypes;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
{
    /// <inheritdoc cref="IDeliveryTaxonomyResponse" />
    internal sealed class DeliveryTaxonomyResponse : AbstractResponse, IDeliveryTaxonomyResponse
    {
        private Lazy<ITaxonomyGroup> _taxonomy;

        /// <inheritdoc/>
        public ITaxonomyGroup Taxonomy
        {
            get => _taxonomy.Value;
            private set => _taxonomy = new Lazy<ITaxonomyGroup>(() => value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a taxonomy group.</param>
        internal DeliveryTaxonomyResponse(ApiResponse response) : base(response)
        {
            _taxonomy = new Lazy<ITaxonomyGroup>(() => new TaxonomyGroup(response.JsonContent), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a taxonomy group.</param>
        /// <param name="taxonomy">A taxonomy group.</param>
        [JsonConstructor]
        internal DeliveryTaxonomyResponse(ApiResponse response, ITaxonomyGroup taxonomy) : base(response)
        {
            Taxonomy = taxonomy;
        }
    }
}
