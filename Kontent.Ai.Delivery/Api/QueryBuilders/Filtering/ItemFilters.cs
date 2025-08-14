using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Concrete implementation of filter builder for content items endpoint.
/// Provides full filtering capabilities including system and element properties with all operators.
/// </summary>
internal sealed class ItemFilters : IItemFilters
{
    public IFilter Equals(string propertyPath, string value)
        => new Filter(propertyPath, FilterOperator.Equals, value);

    public IFilter Equals(string propertyPath, int value)
        => new Filter(propertyPath, FilterOperator.Equals, value);

    public IFilter Equals(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.Equals, value);

    public IFilter Equals(string propertyPath, bool value)
        => new Filter(propertyPath, FilterOperator.Equals, value);

    public IFilter NotEquals(string propertyPath, string value)
        => new Filter(propertyPath, FilterOperator.NotEquals, value);

    public IFilter NotEquals(string propertyPath, int value)
        => new Filter(propertyPath, FilterOperator.NotEquals, value);

    public IFilter NotEquals(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.NotEquals, value);

    public IFilter NotEquals(string propertyPath, bool value)
        => new Filter(propertyPath, FilterOperator.NotEquals, value);

    public IFilter LessThan(string propertyPath, int value)
        => new Filter(propertyPath, FilterOperator.LessThan, value);

    public IFilter LessThan(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.LessThan, value);

    public IFilter LessThanOrEqual(string propertyPath, int value)
        => new Filter(propertyPath, FilterOperator.LessThanOrEqual, value);

    public IFilter LessThanOrEqual(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.LessThanOrEqual, value);

    public IFilter GreaterThan(string propertyPath, int value)
        => new Filter(propertyPath, FilterOperator.GreaterThan, value);

    public IFilter GreaterThan(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.GreaterThan, value);

    public IFilter GreaterThanOrEqual(string propertyPath, int value)
        => new Filter(propertyPath, FilterOperator.GreaterThanOrEqual, value);

    public IFilter GreaterThanOrEqual(string propertyPath, DateTime value)
        => new Filter(propertyPath, FilterOperator.GreaterThanOrEqual, value);

    public IFilter Range(string propertyPath, int lowerBound, int upperBound)
        => new Filter(propertyPath, FilterOperator.Range, new FilterValue.NumericRange(lowerBound, upperBound));

    public IFilter Range(string propertyPath, DateTime lowerBound, DateTime upperBound)
        => new Filter(propertyPath, FilterOperator.Range, new FilterValue.DateRange(lowerBound, upperBound));

    public IFilter In(string propertyPath, params string[] values)
        => new Filter(propertyPath, FilterOperator.In, values);

    public IFilter In(string propertyPath, params int[] values)
        => new Filter(propertyPath, FilterOperator.In, values);

    public IFilter In(string propertyPath, params DateTime[] values)
        => new Filter(propertyPath, FilterOperator.In, values);

    public IFilter NotIn(string propertyPath, params string[] values)
        => new Filter(propertyPath, FilterOperator.NotIn, values);

    public IFilter NotIn(string propertyPath, params int[] values)
        => new Filter(propertyPath, FilterOperator.NotIn, values);

    public IFilter NotIn(string propertyPath, params DateTime[] values)
        => new Filter(propertyPath, FilterOperator.NotIn, values);

    public IFilter Contains(string propertyPath, string value)
        => new Filter(propertyPath, FilterOperator.Contains, value);

    public IFilter Any(string propertyPath, params string[] values)
        => new Filter(propertyPath, FilterOperator.Any, values);

    public IFilter All(string propertyPath, params string[] values)
        => new Filter(propertyPath, FilterOperator.All, values);

    public IFilter Empty(string propertyPath)
        => new Filter(propertyPath, FilterOperator.Empty, FilterValue.EmptyValue);

    public IFilter NotEmpty(string propertyPath)
        => new Filter(propertyPath, FilterOperator.NotEmpty, FilterValue.EmptyValue);
}