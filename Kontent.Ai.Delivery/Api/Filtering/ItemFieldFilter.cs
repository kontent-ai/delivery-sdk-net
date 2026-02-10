namespace Kontent.Ai.Delivery.Api.Filtering;

internal readonly struct ItemFieldFilter<TBuilder>(TBuilder builder, string path, Action<KeyValuePair<string, string>> add)
    : IItemFieldFilter<TBuilder>
{
    public TBuilder IsEqualTo(string value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(double value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(DateTime value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(bool value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));

    public TBuilder IsNotEqualTo(string value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(double value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(DateTime value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(bool value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));

    public TBuilder IsLessThan(double value) => AddValue(FilterSuffix.Lt, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThan(DateTime value) => AddValue(FilterSuffix.Lt, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThan(string value) => AddValue(FilterSuffix.Lt, FilterValueSerializer.Serialize(value));

    public TBuilder IsLessThanOrEqualTo(double value) => AddValue(FilterSuffix.Lte, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThanOrEqualTo(DateTime value) => AddValue(FilterSuffix.Lte, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThanOrEqualTo(string value) => AddValue(FilterSuffix.Lte, FilterValueSerializer.Serialize(value));

    public TBuilder IsGreaterThan(double value) => AddValue(FilterSuffix.Gt, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThan(DateTime value) => AddValue(FilterSuffix.Gt, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThan(string value) => AddValue(FilterSuffix.Gt, FilterValueSerializer.Serialize(value));

    public TBuilder IsGreaterThanOrEqualTo(double value) => AddValue(FilterSuffix.Gte, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThanOrEqualTo(DateTime value) => AddValue(FilterSuffix.Gte, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThanOrEqualTo(string value) => AddValue(FilterSuffix.Gte, FilterValueSerializer.Serialize(value));

    public TBuilder IsWithinRange(double lower, double upper) => AddValue(FilterSuffix.Range, FilterValueSerializer.SerializeRange(lower, upper));
    public TBuilder IsWithinRange(DateTime lower, DateTime upper) => AddValue(FilterSuffix.Range, FilterValueSerializer.SerializeRange(lower, upper));
    public TBuilder IsWithinRange(string lower, string upper) => AddValue(FilterSuffix.Range, FilterValueSerializer.SerializeRange(lower, upper));

    public TBuilder IsIn(params string[] values) => AddValue(FilterSuffix.In, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsIn(params double[] values) => AddValue(FilterSuffix.In, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsIn(params DateTime[] values) => AddValue(FilterSuffix.In, FilterValueSerializer.SerializeArray(values));

    public TBuilder IsNotIn(params string[] values) => AddValue(FilterSuffix.Nin, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsNotIn(params double[] values) => AddValue(FilterSuffix.Nin, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsNotIn(params DateTime[] values) => AddValue(FilterSuffix.Nin, FilterValueSerializer.SerializeArray(values));

    public TBuilder Contains(string value) => AddValue(FilterSuffix.Contains, FilterValueSerializer.Serialize(value));
    public TBuilder ContainsAny(params string[] values) => AddValue(FilterSuffix.Any, FilterValueSerializer.SerializeArray(values));
    public TBuilder ContainsAll(params string[] values) => AddValue(FilterSuffix.All, FilterValueSerializer.SerializeArray(values));

    public TBuilder IsEmpty()
    {
        add(new KeyValuePair<string, string>(path + FilterSuffix.Empty, string.Empty));
        return builder;
    }

    public TBuilder IsNotEmpty()
    {
        add(new KeyValuePair<string, string>(path + FilterSuffix.Nempty, string.Empty));
        return builder;
    }

    private TBuilder AddValue(string suffix, string value)
    {
        add(new KeyValuePair<string, string>(path + suffix, value));
        return builder;
    }
}
