namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Filter operators available for content items fields (system or element).
/// </summary>
/// <typeparam name="TBuilder">Parent builder type to return to for fluent chaining.</typeparam>
public interface IItemFieldFilter<out TBuilder>
{
    // Equality
    TBuilder IsEqualTo(string value);
    TBuilder IsEqualTo(double value);
    TBuilder IsEqualTo(DateTime value);
    TBuilder IsEqualTo(bool value);

    TBuilder IsNotEqualTo(string value);
    TBuilder IsNotEqualTo(double value);
    TBuilder IsNotEqualTo(DateTime value);
    TBuilder IsNotEqualTo(bool value);

    // Comparison
    TBuilder IsLessThan(double value);
    TBuilder IsLessThan(DateTime value);
    TBuilder IsLessThan(string value);

    TBuilder IsLessThanOrEqualTo(double value);
    TBuilder IsLessThanOrEqualTo(DateTime value);
    TBuilder IsLessThanOrEqualTo(string value);

    TBuilder IsGreaterThan(double value);
    TBuilder IsGreaterThan(DateTime value);
    TBuilder IsGreaterThan(string value);

    TBuilder IsGreaterThanOrEqualTo(double value);
    TBuilder IsGreaterThanOrEqualTo(DateTime value);
    TBuilder IsGreaterThanOrEqualTo(string value);

    // Range (inclusive)
    TBuilder IsWithinRange(double lower, double upper);
    TBuilder IsWithinRange(DateTime lower, DateTime upper);
    TBuilder IsWithinRange(string lower, string upper);

    // Collections
    TBuilder IsIn(params string[] values);
    TBuilder IsIn(params double[] values);
    TBuilder IsIn(params DateTime[] values);

    TBuilder IsNotIn(params string[] values);
    TBuilder IsNotIn(params double[] values);
    TBuilder IsNotIn(params DateTime[] values);

    // Text / array operators
    TBuilder Contains(string value);
    TBuilder ContainsAny(params string[] values);
    TBuilder ContainsAll(params string[] values);

    // Empty checks
    TBuilder IsEmpty();
    TBuilder IsNotEmpty();
}


