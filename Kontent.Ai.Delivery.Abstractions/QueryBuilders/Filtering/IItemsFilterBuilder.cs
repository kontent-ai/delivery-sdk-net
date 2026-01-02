namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent filter builder for the content items endpoint.
/// Filters are combined with AND semantics (multiple query parameters).
/// </summary>
public interface IItemsFilterBuilder
{
    /// <summary>
    /// Selects a system property (e.g. <c>type</c>, <c>codename</c>, <c>last_modified</c>).
    /// </summary>
    IItemFieldFilter<IItemsFilterBuilder> System(string propertyName);

    /// <summary>
    /// Selects an element property by codename (e.g. <c>title</c>, <c>price</c>, <c>category</c>).
    /// </summary>
    IItemFieldFilter<IItemsFilterBuilder> Element(string elementCodename);
}


