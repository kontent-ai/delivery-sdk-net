namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent filter builder for the taxonomy groups endpoint.
/// Exposes only operators supported by the Taxonomies API.
/// </summary>
public interface ITaxonomiesFilterBuilder
{
    /// <summary>
    /// Selects a system property (e.g. <c>codename</c>, <c>name</c>, <c>last_modified</c>).
    /// </summary>
    ITaxonomyFieldFilter<ITaxonomiesFilterBuilder> System(string propertyName);
}


