#pragma warning disable CS1591 // Missing XML comment - filter methods are self-documenting

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Filter operators available for content types fields (system only).
/// This is intentionally limited to what the Types endpoint supports.
/// </summary>
/// <typeparam name="TBuilder">Parent builder type to return to for fluent chaining.</typeparam>
public interface ITypeFieldFilter<out TBuilder>
{
    // Equality
    TBuilder IsEqualTo(string value);
    TBuilder IsEqualTo(DateTime value);

    TBuilder IsNotEqualTo(string value);
    TBuilder IsNotEqualTo(DateTime value);

    // Collection (strings only)
    TBuilder IsIn(params string[] values);
    TBuilder IsNotIn(params string[] values);

    // Date comparisons (system.last_modified etc.)
    TBuilder IsWithinRange(DateTime lower, DateTime upper);
    TBuilder IsLessThan(DateTime value);
    TBuilder IsLessThanOrEqualTo(DateTime value);
    TBuilder IsGreaterThan(DateTime value);
    TBuilder IsGreaterThanOrEqualTo(DateTime value);
}


