using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Concrete implementation of filter builder for taxonomy groups endpoint.
/// Provides very limited filtering capabilities - only basic equality for system properties.
/// </summary>
internal sealed class TaxonomyFilters : ITaxonomyFilters
{
    public IFilter Equals(string propertyPath, string value)
        => new Filter(propertyPath, FilterOperator.Equals, value);

    public IFilter NotEquals(string propertyPath, string value)
        => new Filter(propertyPath, FilterOperator.NotEquals, value);

    public IFilter Equals(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.Equals, value);

    public IFilter NotEquals(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.NotEquals, value);
}