namespace Kontent.Ai.Delivery.Api.Filtering;

internal sealed class TypesFilterBuilder(ICollection<KeyValuePair<string, string>> filters) : ITypesFilterBuilder
{
    public ITypeFieldFilter<ITypesFilterBuilder> System(string propertyName)
        => new TypeFieldFilter<ITypesFilterBuilder>(this, FilterPath.System(propertyName), filters.Add);
}
