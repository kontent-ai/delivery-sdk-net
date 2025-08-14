using System;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// Filter builder interface for content types endpoint.
/// Provides limited filtering capabilities - only system properties with basic operators.
/// </summary>
public interface ITypeFilters
{
    // Basic equality operators for system properties
    IFilter Equals(string propertyPath, string value);
    IFilter NotEquals(string propertyPath, string value);

    // Collection operators for system properties
    IFilter In(string propertyPath, params string[] values);
    IFilter NotIn(string propertyPath, params string[] values);

    // Date range operators for system.last_modified
    IFilter Range(string propertyPath, DateTime lowerBound, DateTime upperBound);
    IFilter LessThan(string propertyPath, DateTime value);
    IFilter LessThanOrEqual(string propertyPath, DateTime value);
    IFilter GreaterThan(string propertyPath, DateTime value);
    IFilter GreaterThanOrEqual(string propertyPath, DateTime value);
}