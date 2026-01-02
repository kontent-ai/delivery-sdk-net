namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Filter operators available for taxonomy fields (system only).
/// This is intentionally limited to what the Taxonomies endpoint supports.
/// </summary>
/// <typeparam name="TBuilder">Parent builder type to return to for fluent chaining.</typeparam>
public interface ITaxonomyFieldFilter<out TBuilder>
{
    TBuilder Eq(string value);
    TBuilder Eq(DateTime value);

    TBuilder Neq(string value);
    TBuilder Neq(DateTime value);
}


