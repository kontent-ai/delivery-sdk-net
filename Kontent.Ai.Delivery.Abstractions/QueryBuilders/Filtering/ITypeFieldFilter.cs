namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Filter operators available for content types fields (system only).
/// This is intentionally limited to what the Types endpoint supports.
/// </summary>
/// <typeparam name="TBuilder">Parent builder type to return to for fluent chaining.</typeparam>
public interface ITypeFieldFilter<out TBuilder>
{
    // Equality
    TBuilder Eq(string value);
    TBuilder Eq(DateTime value);

    TBuilder Neq(string value);
    TBuilder Neq(DateTime value);

    // Collection (strings only)
    TBuilder In(params string[] values);
    TBuilder Nin(params string[] values);

    // Date comparisons (system.last_modified etc.)
    TBuilder Range(DateTime lower, DateTime upper);
    TBuilder Lt(DateTime value);
    TBuilder Lte(DateTime value);
    TBuilder Gt(DateTime value);
    TBuilder Gte(DateTime value);
}


