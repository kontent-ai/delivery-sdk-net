using System;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// Filter builder interface for taxonomy groups endpoint.
/// Provides very limited filtering capabilities - only basic equality for system properties.
/// </summary>
public interface ITaxonomyFilters
{
    /// <summary>
    /// Retrieves taxonomy groups where the specified system property equals the specified value.
    /// </summary>
    IFilter Equals(TaxonomySystemPath path, string value);
    /// <summary>
    /// Retrieves taxonomy groups where the specified system property does not equal the specified value.
    /// </summary>
    IFilter NotEquals(TaxonomySystemPath path, string value);

    /// <inheritdoc cref="Equals(TaxonomySystemPath, string)"/>
    IFilter Equals(TaxonomySystemPath path, DateTime value);
    /// <inheritdoc cref="NotEquals(TaxonomySystemPath, string)"/>
    IFilter NotEquals(TaxonomySystemPath path, DateTime value);
}