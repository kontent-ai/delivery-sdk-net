namespace Kontent.Ai.Delivery.Api.Filtering;

internal readonly struct TypeFieldFilter<TBuilder>(TBuilder builder, string path, Action<KeyValuePair<string, string>> add)
    : ITypeFieldFilter<TBuilder>
{
    public TBuilder IsEqualTo(string value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(DateTime value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));

    public TBuilder IsNotEqualTo(string value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(DateTime value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));

    public TBuilder IsIn(params string[] values) => AddValue(FilterSuffix.In, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsNotIn(params string[] values) => AddValue(FilterSuffix.Nin, FilterValueSerializer.SerializeArray(values));

    public TBuilder IsWithinRange(DateTime lower, DateTime upper) => AddValue(FilterSuffix.Range, FilterValueSerializer.SerializeRange(lower, upper));
    public TBuilder IsLessThan(DateTime value) => AddValue(FilterSuffix.Lt, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThanOrEqualTo(DateTime value) => AddValue(FilterSuffix.Lte, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThan(DateTime value) => AddValue(FilterSuffix.Gt, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThanOrEqualTo(DateTime value) => AddValue(FilterSuffix.Gte, FilterValueSerializer.Serialize(value));

    private TBuilder AddValue(string suffix, string value)
    {
        add(new KeyValuePair<string, string>(path + suffix, value));
        return builder;
    }
}
