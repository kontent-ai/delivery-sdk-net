using Kentico.Kontent.Delivery.Abstractions.Models.Taxonomy;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a taxonomy group.
    /// </summary>
    public interface IDeliveryTaxonomyResponse : IResponse
    {
        /// <summary>
        /// Gets the taxonomy group.
        /// </summary>
        ITaxonomyGroup Taxonomy { get; }
    }
}