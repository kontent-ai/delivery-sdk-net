using System;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// Filter builder interface for content types endpoint.
/// Provides limited filtering capabilities - only system properties with basic operators.
/// </summary>
public interface ITypeFilters
{
    // Basic equality operators for system properties
    /// <summary>
    /// Retrieves types where the specified system property equals the specified value.
    /// </summary>
    IFilter Equals(TypeSystemPath path, string value);
    /// <inheritdoc cref="Equals(TypeSystemPath, string)"/>
    IFilter Equals(TypeSystemPath path, DateTime value);

    /// <summary>
    /// Retrieves types where the specified system property does not equal the specified value.
    /// </summary>
    IFilter NotEquals(TypeSystemPath path, string value);
    /// <inheritdoc cref="NotEquals(TypeSystemPath, string)"/>
    IFilter NotEquals(TypeSystemPath path, DateTime value);

    // Collection operators for system properties
    /// <summary>
    /// Retrieves types where the specified system property is in the specified collection.
    /// </summary>
    IFilter In(TypeSystemPath path, params string[] values);
    /// <summary>
    /// Retrieves types where the specified system property is not in the specified collection.
    /// </summary>
    IFilter NotIn(TypeSystemPath path, params string[] values);

    // Date range operators for system.last_modified
    /// <summary>
    /// Retrieves types where the specified system property is within the specified range.
    /// </summary>
    IFilter Range(TypeSystemPath path, DateTime lowerBound, DateTime upperBound);
    /// <summary>
    /// Retrieves types where the specified system property is less than the specified value.
    /// </summary>
    IFilter LessThan(TypeSystemPath path, DateTime value);
    /// <summary>
    /// Retrieves types where the specified system property is less than or equal to the specified value.
    /// </summary>
    IFilter LessThanOrEqual(TypeSystemPath path, DateTime value);
    /// <summary>
    /// Retrieves types where the specified system property is greater than the specified value.
    /// </summary>
    IFilter GreaterThan(TypeSystemPath path, DateTime value);
    /// <summary>
    /// Retrieves types where the specified system property is greater than or equal to the specified value.
    /// </summary>
    IFilter GreaterThanOrEqual(TypeSystemPath path, DateTime value);
}