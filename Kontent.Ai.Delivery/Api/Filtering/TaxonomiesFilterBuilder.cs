namespace Kontent.Ai.Delivery.Api.Filtering;

internal sealed class TaxonomiesFilterBuilder(ICollection<KeyValuePair<string, string>> filters) : ITaxonomiesFilterBuilder
{
    public ITaxonomyFieldFilter<ITaxonomiesFilterBuilder> System(string propertyName)
        => new TaxonomyFieldFilter<ITaxonomiesFilterBuilder>(this, FilterPath.System(propertyName), filters.Add);
}
