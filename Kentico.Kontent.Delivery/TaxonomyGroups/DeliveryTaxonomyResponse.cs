using System;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentTypes;
using Kentico.Kontent.Delivery.SharedModels;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
{
    /// <inheritdoc cref="IDeliveryTaxonomyResponse" />
    public sealed class DeliveryTaxonomyResponse : AbstractResponse, IDeliveryTaxonomyResponse
    {
        private readonly Lazy<TaxonomyGroup> _taxonomy;

        /// <inheritdoc/>
        public ITaxonomyGroup Taxonomy => _taxonomy.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a taxonomy group.</param>
        internal DeliveryTaxonomyResponse(ApiResponse response) : base(response)
        {
            _taxonomy = new Lazy<TaxonomyGroup>(() => new TaxonomyGroup(response.JsonContent), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
