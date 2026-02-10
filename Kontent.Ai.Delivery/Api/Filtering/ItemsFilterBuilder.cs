namespace Kontent.Ai.Delivery.Api.Filtering;

internal sealed class ItemsFilterBuilder(ICollection<KeyValuePair<string, string>> filters) : IItemsFilterBuilder
{
    public IItemFieldFilter<IItemsFilterBuilder> System(string propertyName)
        => new ItemFieldFilter<IItemsFilterBuilder>(this, FilterPath.System(propertyName), filters.Add);

    public IItemFieldFilter<IItemsFilterBuilder> Element(string elementCodename)
        => new ItemFieldFilter<IItemsFilterBuilder>(this, FilterPath.Element(elementCodename), filters.Add);
}
