using System;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// Filter builder interface for content items endpoint.
/// Provides full filtering capabilities including system and element properties with all operators.
/// </summary>
public interface IItemFilters
{
    /// <summary>
    /// Retrieves items where the specified property equals the specified value.
    /// </summary>
    IFilter Equals(string propertyPath, string value);
    /// <inheritdoc cref="Equals(string, string)"/>
    IFilter Equals(string propertyPath, double value);
    /// <inheritdoc cref="Equals(string, string)"/>
    IFilter Equals(string propertyPath, DateTime value);
    /// <inheritdoc cref="Equals(string, string)"/>
    IFilter Equals(string propertyPath, bool value);

    /// <summary>
    /// Retrieves items where the specified property does not equal the specified value.
    /// </summary>
    IFilter NotEquals(string propertyPath, string value);
    /// <inheritdoc cref="NotEquals(string, string)"/>
    IFilter NotEquals(string propertyPath, double value);
    /// <inheritdoc cref="NotEquals(string, string)"/>
    IFilter NotEquals(string propertyPath, DateTime value);
    /// <inheritdoc cref="NotEquals(string, string)"/>
    IFilter NotEquals(string propertyPath, bool value);

    /// <summary>
    /// Retrieves items where the specified property is less than the specified value.
    /// </summary>
    IFilter LessThan(string propertyPath, double value);
    /// <inheritdoc cref="LessThan(string, double)"/>
    IFilter LessThan(string propertyPath, DateTime value);

    /// <summary>
    /// Retrieves items where the specified property is less than or equal to the specified value.
    /// </summary>
    IFilter LessThanOrEqual(string propertyPath, double value);
    /// <inheritdoc cref="LessThanOrEqual(string, double)"/>
    IFilter LessThanOrEqual(string propertyPath, DateTime value);

    /// <summary>
    /// Retrieves items where the specified property is greater than the specified value.
    /// </summary>
    IFilter GreaterThan(string propertyPath, double value);
    /// <inheritdoc cref="GreaterThan(string, double)"/>
    IFilter GreaterThan(string propertyPath, DateTime value);

    /// <summary>
    /// Retrieves items where the specified property is greater than or equal to the specified value.
    /// </summary>
    IFilter GreaterThanOrEqual(string propertyPath, double value);
    /// <inheritdoc cref="GreaterThanOrEqual(string, double)"/>
    IFilter GreaterThanOrEqual(string propertyPath, DateTime value);

    /// <summary>
    /// Retrieves items where the specified property is within the specified range.
    /// </summary>
    IFilter Range(string propertyPath, double lowerBound, double upperBound);
    /// <inheritdoc cref="Range(string, double, double)"/>
    IFilter Range(string propertyPath, DateTime lowerBound, DateTime upperBound);

    /// <summary>
    /// Retrieves items where the specified property is in the specified collection.
    /// </summary>
    IFilter In(string propertyPath, params string[] values);
    /// <inheritdoc cref="In(string, string[])"/>
    IFilter In(string propertyPath, params double[] values);
    /// <inheritdoc cref="In(string, string[])"/>
    IFilter In(string propertyPath, params DateTime[] values);
    /// <inheritdoc cref="In(string, string[])"/>

    /// <summary>
    /// Retrieves items where the specified property is not in the specified collection.
    /// </summary>
    IFilter NotIn(string propertyPath, params string[] values);
    /// <inheritdoc cref="NotIn(string, string[])"/>
    IFilter NotIn(string propertyPath, params double[] values);
    /// <inheritdoc cref="NotIn(string, string[])"/>
    IFilter NotIn(string propertyPath, params DateTime[] values);

    /// <summary>
    /// Retrieves items where the specified array property contains the specified value.
    /// </summary>
    IFilter Contains(string propertyPath, string value);

    /// <summary>
    /// Retrieves items where the specified array property contains any of the specified values.
    /// </summary>
    IFilter Any(string propertyPath, params string[] values);
    /// <summary>
    /// Retrieves items where the specified array property contains all of the specified values.
    /// </summary>
    IFilter All(string propertyPath, params string[] values);

    /// <summary>
    /// Retrieves items where the specified property is empty.
    /// </summary>
    IFilter Empty(string propertyPath);
    /// <summary>
    /// Retrieves items where the specified property is not empty.
    /// </summary>
    IFilter NotEmpty(string propertyPath);
}