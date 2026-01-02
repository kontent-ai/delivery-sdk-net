namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Filter operators available for content items fields (system or element).
/// </summary>
/// <typeparam name="TBuilder">Parent builder type to return to for fluent chaining.</typeparam>
public interface IItemFieldFilter<out TBuilder>
{
    // Equality
    TBuilder Eq(string value);
    TBuilder Eq(double value);
    TBuilder Eq(DateTime value);
    TBuilder Eq(bool value);

    TBuilder Neq(string value);
    TBuilder Neq(double value);
    TBuilder Neq(DateTime value);
    TBuilder Neq(bool value);

    // Comparison
    TBuilder Lt(double value);
    TBuilder Lt(DateTime value);
    TBuilder Lt(string value);

    TBuilder Lte(double value);
    TBuilder Lte(DateTime value);
    TBuilder Lte(string value);

    TBuilder Gt(double value);
    TBuilder Gt(DateTime value);
    TBuilder Gt(string value);

    TBuilder Gte(double value);
    TBuilder Gte(DateTime value);
    TBuilder Gte(string value);

    // Range (inclusive)
    TBuilder Range(double lower, double upper);
    TBuilder Range(DateTime lower, DateTime upper);
    TBuilder Range(string lower, string upper);

    // Collections
    TBuilder In(params string[] values);
    TBuilder In(params double[] values);
    TBuilder In(params DateTime[] values);

    TBuilder Nin(params string[] values);
    TBuilder Nin(params double[] values);
    TBuilder Nin(params DateTime[] values);

    // Text / array operators
    TBuilder Contains(string value);
    TBuilder Any(params string[] values);
    TBuilder All(params string[] values);

    // Empty checks
    TBuilder Empty();
    TBuilder Nempty();
}


