#pragma warning disable CS1591 // Missing XML comment - filter methods are self-documenting

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Filter operators available for taxonomy fields (system only).
/// This is intentionally limited to what the Taxonomies endpoint supports.
/// </summary>
/// <typeparam name="TBuilder">Parent builder type to return to for fluent chaining.</typeparam>
public interface ITaxonomyFieldFilter<out TBuilder>
{
    TBuilder IsEqualTo(string value);
    TBuilder IsEqualTo(DateTime value);

    TBuilder IsNotEqualTo(string value);
    TBuilder IsNotEqualTo(DateTime value);
}
