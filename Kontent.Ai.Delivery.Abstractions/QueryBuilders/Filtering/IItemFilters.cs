// =============================================================================
// OneOf Type Aliases for Type-Safe Filtering
// =============================================================================
// These aliases provide compile-time type safety for filter parameter values.
//
// ScalarValue: Any scalar value (string, double, DateTime, or bool)
//   - Used for equality/inequality filters
//   - Example: f.Equals(path, "text"), f.Equals(path, 42.0)
//
// ComparableValue: Values supporting ordering (double, DateTime, or string)
//   - Used for comparison operators (<, >, <=, >=)
//   - Example: f.LessThan(path, 100.0), f.GreaterThan(path, DateTime.Now)
//
// RangeBounds: Range boundaries as tuples (numeric or date)
//   - Used for range queries with inclusive bounds
//   - Example: f.Range(path, (10.0, 100.0)), f.Range(path, (start, end))
// =============================================================================

using ScalarValue = OneOf.OneOf<string, double, System.DateTime, bool>;
using ComparableValue = OneOf.OneOf<double, System.DateTime, string>;
using RangeBounds = OneOf.OneOf<(double Lower, double Upper), (System.DateTime Lower, System.DateTime Upper)>;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Filter builder interface for content items endpoint.
/// Provides all 13 filtering operators for comprehensive content querying.
/// </summary>
public interface IItemFilters
{
    /// <summary>
    /// Filters items where the specified property equals the given value.
    /// </summary>
    /// <param name="path">The property path to filter on (system or element property).</param>
    /// <param name="value">The value to match (string, number, date, or boolean).</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter Equals(IPropertyPath path, ScalarValue value);

    /// <summary>
    /// Filters items where the specified property does not equal the given value.
    /// </summary>
    /// <param name="path">The property path to filter on (system or element property).</param>
    /// <param name="value">The value to exclude (string, number, date, or boolean).</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter NotEquals(IPropertyPath path, ScalarValue value);

    /// <summary>
    /// Filters items where the specified property is less than the given value.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="value">The upper bound (exclusive) for comparison.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter LessThan(IPropertyPath path, ComparableValue value);

    /// <summary>
    /// Filters items where the specified property is less than or equal to the given value.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="value">The upper bound (inclusive) for comparison.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter LessThanOrEqual(IPropertyPath path, ComparableValue value);

    /// <summary>
    /// Filters items where the specified property is greater than the given value.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="value">The lower bound (exclusive) for comparison.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter GreaterThan(IPropertyPath path, ComparableValue value);

    /// <summary>
    /// Filters items where the specified property is greater than or equal to the given value.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="value">The lower bound (inclusive) for comparison.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter GreaterThanOrEqual(IPropertyPath path, ComparableValue value);

    /// <summary>
    /// Filters items where the specified property falls within the given range (inclusive).
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="range">The range boundaries as a tuple (lower, upper) - both bounds are inclusive.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter Range(IPropertyPath path, RangeBounds range);

    /// <summary>
    /// Filters items where the specified property matches any value in the provided collection of strings.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="values">The collection of string values to match against.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter In(IPropertyPath path, string[] values);

    /// <summary>
    /// Filters items where the specified property does not match any value in the provided collection of strings.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="values">The collection of string values to exclude.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter NotIn(IPropertyPath path, string[] values);

    /// <summary>
    /// Filters items where the specified property matches any value in the provided collection of numbers.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="values">The collection of numeric values to match against.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter In(IPropertyPath path, double[] values);

    /// <summary>
    /// Filters items where the specified property does not match any value in the provided collection of numbers.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="values">The collection of numeric values to exclude.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter NotIn(IPropertyPath path, double[] values);

    /// <summary>
    /// Filters items where the specified property matches any value in the provided collection of dates.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="values">The collection of date values to match against.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter In(IPropertyPath path, System.DateTime[] values);

    /// <summary>
    /// Filters items where the specified property does not match any value in the provided collection of dates.
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <param name="values">The collection of date values to exclude.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter NotIn(IPropertyPath path, System.DateTime[] values);

    /// <summary>
    /// Filters items where the specified text property contains the given substring.
    /// </summary>
    /// <param name="path">The property path to filter on (must be a text property).</param>
    /// <param name="value">The substring to search for (case-insensitive).</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter Contains(IPropertyPath path, string value);

    /// <summary>
    /// Filters items where the specified array property contains at least one of the given values.
    /// </summary>
    /// <param name="path">The property path to filter on (must be a multi-value property).</param>
    /// <param name="values">The values to check for (matches if any value is present).</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter Any(IPropertyPath path, params string[] values);

    /// <summary>
    /// Filters items where the specified array property contains all of the given values.
    /// </summary>
    /// <param name="path">The property path to filter on (must be a multi-value property).</param>
    /// <param name="values">The values to check for (matches only if all values are present).</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter All(IPropertyPath path, params string[] values);

    /// <summary>
    /// Filters items where the specified property has no value (is null or empty).
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter Empty(IPropertyPath path);

    /// <summary>
    /// Filters items where the specified property has a value (is not null or empty).
    /// </summary>
    /// <param name="path">The property path to filter on.</param>
    /// <returns>A filter that can be applied to a query.</returns>
    IFilter NotEmpty(IPropertyPath path);
}