using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Concrete implementation of filter builder for content items endpoint.
/// Provides full filtering capabilities including system and element properties with all operators.
/// </summary>
internal sealed class ItemFilters : IItemFilters
{
    public IFilter Equals(IPropertyPath path, Scalar value)
        => new Filter(path.Serialize(), FilterOperator.Equals, FilterValueMapper.From(value));

    public IFilter NotEquals(IPropertyPath path, Scalar value)
        => new Filter(path.Serialize(), FilterOperator.NotEquals, FilterValueMapper.From(value));

    public IFilter LessThan(IPropertyPath path, Comparable value)
        => new Filter(path.Serialize(), FilterOperator.LessThan, FilterValueMapper.From(value));

    public IFilter LessThanOrEqual(IPropertyPath path, Comparable value)
        => new Filter(path.Serialize(), FilterOperator.LessThanOrEqual, FilterValueMapper.From(value));

    public IFilter GreaterThan(IPropertyPath path, Comparable value)
        => new Filter(path.Serialize(), FilterOperator.GreaterThan, FilterValueMapper.From(value));

    public IFilter GreaterThanOrEqual(IPropertyPath path, Comparable value)
        => new Filter(path.Serialize(), FilterOperator.GreaterThanOrEqual, FilterValueMapper.From(value));

    public IFilter Range(IPropertyPath path, RangeTuple bounds)
        => new Filter(path.Serialize(), FilterOperator.Range, FilterValueMapper.From(bounds));

    public IFilter In(IPropertyPath path, ScalarArray values)
        => new Filter(path.Serialize(), FilterOperator.In, FilterValueMapper.From(values));

    public IFilter NotIn(IPropertyPath path, ScalarArray values)
        => new Filter(path.Serialize(), FilterOperator.NotIn, FilterValueMapper.From(values));

    public IFilter Contains(IPropertyPath path, string value)
        => new Filter(path.Serialize(), FilterOperator.Contains, StringValue.From(value));

    public IFilter Any(IPropertyPath path, params string[] values)
        => new Filter(path.Serialize(), FilterOperator.Any, StringArrayValue.From(values));

    public IFilter All(IPropertyPath path, params string[] values)
        => new Filter(path.Serialize(), FilterOperator.All, StringArrayValue.From(values));

    public IFilter Empty(IPropertyPath path)
        => new Filter(path.Serialize(), FilterOperator.Empty, EmptyValue.From(string.Empty));

    public IFilter NotEmpty(IPropertyPath path)
        => new Filter(path.Serialize(), FilterOperator.NotEmpty, EmptyValue.From(string.Empty));

}
