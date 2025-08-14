namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// Base interface for all content filtering operations.
/// </summary>
public interface IFilter
{
    /// <summary>
    /// The property path this filter applies to (e.g., "system.type", "elements.title").
    /// </summary>
    string PropertyPath { get; }

    /// <summary>
    /// Serializes this filter to the Kontent.ai API query parameter format.
    /// </summary>
    /// <returns>The serialized filter string.</returns>
    string ToQueryParameter();
}