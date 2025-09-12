using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

/// <inheritdoc cref="IDeliveryTaxonomyResponse" />
internal sealed class DeliveryTaxonomyResponse : IDeliveryTaxonomyResponse
{
    /// <inheritdoc/>
    public ITaxonomyGroup Taxonomy
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryTaxonomyResponse"/> class.
    /// </summary>
    /// <param name="taxonomy">A taxonomy group.</param>
    [JsonConstructor]
    internal DeliveryTaxonomyResponse(ITaxonomyGroup taxonomy)
    {
        Taxonomy = taxonomy;
    }
}
