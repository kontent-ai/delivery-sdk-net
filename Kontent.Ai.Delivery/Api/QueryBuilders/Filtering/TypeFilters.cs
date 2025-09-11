namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Concrete implementation of filter builder for content types endpoint.
/// Provides limited filtering capabilities - only system properties with basic operators.
/// </summary>
internal sealed class TypeFilters : ITypeFilters
{
    public IFilter Equals(TypeSystemPath path, string value)
        => new Filter(path.Serialize(), FilterOperator.Equals, StringValue.From(value));

    public IFilter Equals(TypeSystemPath path, DateTime value)
        => new Filter(path.Serialize(), FilterOperator.Equals, DateTimeValue.From(value));

    public IFilter NotEquals(TypeSystemPath path, string value)
        => new Filter(path.Serialize(), FilterOperator.NotEquals, StringValue.From(value));

    public IFilter NotEquals(TypeSystemPath path, DateTime value)
        => new Filter(path.Serialize(), FilterOperator.NotEquals, DateTimeValue.From(value));

    public IFilter In(TypeSystemPath path, params string[] values)
        => new Filter(path.Serialize(), FilterOperator.In, StringArrayValue.From(values));

    public IFilter NotIn(TypeSystemPath path, params string[] values)
        => new Filter(path.Serialize(), FilterOperator.NotIn, StringArrayValue.From(values));

    public IFilter Range(TypeSystemPath path, DateTime lowerBound, DateTime upperBound)
        => new Filter(path.Serialize(), FilterOperator.Range, DateRangeValue.From((lowerBound, upperBound)));

    public IFilter LessThan(TypeSystemPath path, DateTime value)
        => new Filter(path.Serialize(), FilterOperator.LessThan, DateTimeValue.From(value));

    public IFilter LessThanOrEqual(TypeSystemPath path, DateTime value)
        => new Filter(path.Serialize(), FilterOperator.LessThanOrEqual, DateTimeValue.From(value));

    public IFilter GreaterThan(TypeSystemPath path, DateTime value)
        => new Filter(path.Serialize(), FilterOperator.GreaterThan, DateTimeValue.From(value));

    public IFilter GreaterThanOrEqual(TypeSystemPath path, DateTime value)
        => new Filter(path.Serialize(), FilterOperator.GreaterThanOrEqual, DateTimeValue.From(value));
}