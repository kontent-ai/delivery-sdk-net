namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a delta update.
/// </summary>
public interface ISyncV2TaxonomyData
{
    /// <summary>
    /// Gets the system attributes of the taxonomy group.
    /// </summary>
    public ITaxonomyGroupSystemAttributes System { get; }
}
