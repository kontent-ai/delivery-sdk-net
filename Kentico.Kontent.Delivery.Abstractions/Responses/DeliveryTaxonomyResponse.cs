using System;
using System.Threading;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a taxonomy group.
    /// </summary>
    public sealed class DeliveryTaxonomyResponse : AbstractResponse
    {
        private readonly Lazy<TaxonomyGroup> _taxonomy;

        /// <summary>
        /// Gets the taxonomy group.
        /// </summary>
        public TaxonomyGroup Taxonomy => _taxonomy.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a taxonomy group.</param>
        internal DeliveryTaxonomyResponse(ApiResponse response) : base(response)
        {
            _taxonomy = new Lazy<TaxonomyGroup>(() => new TaxonomyGroup(_response.Content), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Implicitly converts the specified <paramref name="response"/> to a taxonomy group.
        /// </summary>
        /// <param name="response">The response to convert.</param>
        public static implicit operator TaxonomyGroup(DeliveryTaxonomyResponse response) => response.Taxonomy;
    }
}
