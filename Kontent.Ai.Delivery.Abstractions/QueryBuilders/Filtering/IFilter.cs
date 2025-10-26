namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Base interface for all content filtering operations.
/// </summary>
public interface IFilter
{
    /// <summary>
    /// The property path this filter applies to (e.g., "system.type", "elements.title").
    /// </summary>
    string PropertyPath { get; }

    // <summary>
    /// Gets the filter operator.
    /// </summary>
    FilterOperator Operator { get; }

    /// <summary>
    /// Gets the filter value (null for Empty/NotEmpty operators).
    /// </summary>
    IFilterValue? Value { get; }
}