namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Concrete implementation of IFilter that represents a filtering operation.
/// </summary>
public sealed record Filter : IFilter
{
    /// <summary>
    /// Gets the property path this filter applies to.
    /// </summary>
    public string PropertyPath { get; }

    /// <summary>
    /// Gets the filter operator.
    /// </summary>
    public FilterOperator Operator { get; }

    /// <summary>
    /// Gets the filter value (null for Empty/NotEmpty operators).
    /// </summary>
    public IFilterValue? Value { get; }

    /// <summary>
    /// Creates a new filter with validation.
    /// </summary>
    /// <param name="propertyPath">The property path to filter on (e.g., "system.type" or "elements.title").</param>
    /// <param name="operator">The filter operator to apply.</param>
    /// <param name="value">The value to filter by (must be null for Empty/NotEmpty operators).</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public Filter(string propertyPath, FilterOperator @operator, IFilterValue? value = null)
    {
        // Validate property path
        if (string.IsNullOrWhiteSpace(propertyPath))
        {
            throw new ArgumentException("Property path cannot be null or empty.", nameof(propertyPath));
        }

        // Validate operator-value compatibility
        ValidateOperatorValueCompatibility(@operator, value);

        PropertyPath = propertyPath;
        Operator = @operator;
        Value = value;
    }

    /// <summary>
    /// Validates that the operator and value are compatible.
    /// </summary>
    private static void ValidateOperatorValueCompatibility(FilterOperator @operator, IFilterValue? value)
    {
        var isEmptyOperator = @operator is FilterOperator.Empty or FilterOperator.NotEmpty;

        if (isEmptyOperator && value is not null)
        {
            throw new ArgumentException(
                $"The {@operator} operator must not have a value. Use null for the value parameter.",
                nameof(value));
        }

        if (!isEmptyOperator && value is null)
        {
            throw new ArgumentException(
                $"The {@operator} operator requires a value. Provide a non-null FilterValue.",
                nameof(value));
        }
    }
}