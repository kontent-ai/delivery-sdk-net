using System;
using System.Threading;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a taxonomy group.
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
        /// <param name="response">The response from Kentico Cloud Delivery API that contains a list of taxonomy groups.</param>
        internal DeliveryTaxonomyResponse(ApiResponse response) : base(response)
        {
            _taxonomy = new Lazy<TaxonomyGroup>(() => new TaxonomyGroup(_response.Content), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Implictly converts the specified <paramref name="response"/> to a taxonomy group.
        /// </summary>
        /// <remarks>
        /// This conversion provides backward compatibility with previous versions of <see cref="DeliveryClient"/> where response was represented as <see cref="TaxonomyGroup"/>.
        /// </remarks>
        /// <param name="response">The response to convert.</param>
        public static implicit operator TaxonomyGroup(DeliveryTaxonomyResponse response) => response.Taxonomy;
    }
}
