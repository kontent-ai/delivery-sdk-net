namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a taxonomy group.
/// Internal: Used only for JSON deserialization, then mapped to domain models.
/// </summary>
internal interface IDeliveryTaxonomyResponse
{
    /// <summary>
    /// Gets the taxonomy group.
    /// </summary>
    ITaxonomyGroup Taxonomy { get; }
}