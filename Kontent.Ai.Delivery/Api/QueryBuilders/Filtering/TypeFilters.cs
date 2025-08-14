using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Concrete implementation of filter builder for content types endpoint.
/// Provides limited filtering capabilities - only system properties with basic operators.
/// </summary>
internal sealed class TypeFilters : ITypeFilters
{
    public IFilter Equals(string propertyPath, string value)
        => new Filter(propertyPath, FilterOperator.Equals, value);

    public IFilter NotEquals(string propertyPath, string value)
        => new Filter(propertyPath, FilterOperator.NotEquals, value);

    public IFilter In(string propertyPath, params string[] values)
        => new Filter(propertyPath, FilterOperator.In, values);

    public IFilter NotIn(string propertyPath, params string[] values)
        => new Filter(propertyPath, FilterOperator.NotIn, values);

    public IFilter Range(string propertyPath, DateTime lowerBound, DateTime upperBound)
        => new Filter(propertyPath, FilterOperator.Range, new FilterValue.DateRange(lowerBound, upperBound));

    public IFilter LessThan(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.LessThan, value);

    public IFilter LessThanOrEqual(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.LessThanOrEqual, value);

    public IFilter GreaterThan(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.GreaterThan, value);

    public IFilter GreaterThanOrEqual(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.GreaterThanOrEqual, value);
}