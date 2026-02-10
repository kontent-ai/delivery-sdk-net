namespace Kontent.Ai.Delivery.Api.Filtering;

internal readonly struct TaxonomyFieldFilter<TBuilder>(TBuilder builder, string path, Action<KeyValuePair<string, string>> add)
    : ITaxonomyFieldFilter<TBuilder>
{
    public TBuilder IsEqualTo(string value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(DateTime value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));

    public TBuilder IsNotEqualTo(string value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(DateTime value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));

    private TBuilder AddValue(string suffix, string value)
    {
        add(new KeyValuePair<string, string>(path + suffix, value));
        return builder;
    }
}
