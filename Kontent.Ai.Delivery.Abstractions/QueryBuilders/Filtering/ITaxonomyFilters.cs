using System;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// Filter builder interface for taxonomy groups endpoint.
/// Provides very limited filtering capabilities - only basic equality for system properties.
/// </summary>
public interface ITaxonomyFilters
{
    /// <summary>
    /// Retrieves taxonomy groups where the specified property equals the specified value.
    /// </summary>
    IFilter Equals(string propertyPath, string value);
    /// <summary>
    /// Retrieves taxonomy groups where the specified property does not equal the specified value.
    /// </summary>
    IFilter NotEquals(string propertyPath, string value);

    /// <inheritdoc cref="Equals(string, string)"/>
    IFilter Equals(string propertyPath, DateTime value);
    /// <inheritdoc cref="NotEquals(string, string)"/>
    IFilter NotEquals(string propertyPath, DateTime value);
}