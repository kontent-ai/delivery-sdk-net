namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Concrete implementation of filter builder for content items endpoint.
/// Provides full filtering capabilities including system and element properties with all operators.
/// </summary>
internal sealed class ItemFilters : IItemFilters
{
    public IFilter Equals(IPropertyPath path, ScalarValue value)
        => new Filter(path.Serialize(), FilterOperator.Equals, FilterValueMapper.From(value));

    public IFilter NotEquals(IPropertyPath path, ScalarValue value)
        => new Filter(path.Serialize(), FilterOperator.NotEquals, FilterValueMapper.From(value));

    public IFilter LessThan(IPropertyPath path, ComparableValue value)
        => new Filter(path.Serialize(), FilterOperator.LessThan, FilterValueMapper.From(value));

    public IFilter LessThanOrEqual(IPropertyPath path, ComparableValue value)
        => new Filter(path.Serialize(), FilterOperator.LessThanOrEqual, FilterValueMapper.From(value));

    public IFilter GreaterThan(IPropertyPath path, ComparableValue value)
        => new Filter(path.Serialize(), FilterOperator.GreaterThan, FilterValueMapper.From(value));

    public IFilter GreaterThanOrEqual(IPropertyPath path, ComparableValue value)
        => new Filter(path.Serialize(), FilterOperator.GreaterThanOrEqual, FilterValueMapper.From(value));

    public IFilter Range(IPropertyPath path, RangeBounds bounds)
        => new Filter(path.Serialize(), FilterOperator.Range, FilterValueMapper.From(bounds));

    public IFilter Contains(IPropertyPath path, string value)
        => new Filter(path.Serialize(), FilterOperator.Contains, StringValue.From(value));

    public IFilter Any(IPropertyPath path, params string[] values)
        => new Filter(path.Serialize(), FilterOperator.Any, StringArrayValue.From(values));

    public IFilter All(IPropertyPath path, params string[] values)
        => new Filter(path.Serialize(), FilterOperator.All, StringArrayValue.From(values));

    public IFilter Empty(IPropertyPath path)
        => new Filter(path.Serialize(), FilterOperator.Empty, null);

    public IFilter NotEmpty(IPropertyPath path)
        => new Filter(path.Serialize(), FilterOperator.NotEmpty, null);

    public IFilter In(IPropertyPath path, string[] values)
        => new Filter(path.Serialize(), FilterOperator.In, StringArrayValue.From(values));

    public IFilter NotIn(IPropertyPath path, string[] values)
        => new Filter(path.Serialize(), FilterOperator.NotIn, StringArrayValue.From(values));

    public IFilter In(IPropertyPath path, double[] values)
        => new Filter(path.Serialize(), FilterOperator.In, NumericArrayValue.From(values));

    public IFilter NotIn(IPropertyPath path, double[] values)
        => new Filter(path.Serialize(), FilterOperator.NotIn, NumericArrayValue.From(values));

    public IFilter In(IPropertyPath path, System.DateTime[] values)
        => new Filter(path.Serialize(), FilterOperator.In, DateTimeArrayValue.From(values));

    public IFilter NotIn(IPropertyPath path, System.DateTime[] values)
        => new Filter(path.Serialize(), FilterOperator.NotIn, DateTimeArrayValue.From(values));

}
