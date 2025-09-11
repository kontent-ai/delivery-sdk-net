namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Concrete implementation of filter builder for taxonomy groups endpoint.
/// Provides very limited filtering capabilities - only basic equality for system properties.
/// </summary>
internal sealed class TaxonomyFilters : ITaxonomyFilters
{
    public IFilter Equals(TaxonomySystemPath path, string value)
        => new Filter(path.Serialize(), FilterOperator.Equals, StringValue.From(value));

    public IFilter NotEquals(TaxonomySystemPath path, string value)
        => new Filter(path.Serialize(), FilterOperator.NotEquals, StringValue.From(value));

    public IFilter Equals(TaxonomySystemPath path, DateTime value)
        => new Filter(path.Serialize(), FilterOperator.Equals, DateTimeValue.From(value));

    public IFilter NotEquals(TaxonomySystemPath path, DateTime value)
        => new Filter(path.Serialize(), FilterOperator.NotEquals, DateTimeValue.From(value));
}