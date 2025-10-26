namespace Kontent.Ai.Delivery.Extensions;

/// <summary>
/// Internal helpers for working with <see cref="IFilter"/> without exposing serialization on the public interface.
/// </summary>
internal static class FilterExtensions
{
    /// <summary>
    /// Serializes an <see cref="IFilter"/> into the Kontent.ai API query parameter format.
    /// </summary>
    internal static KeyValuePair<string, string> ToQueryParameter(this IFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var operatorSuffix = filter.Operator switch
        {
            FilterOperator.Equals => "[eq]",
            FilterOperator.NotEquals => "[neq]",
            FilterOperator.LessThan => "[lt]",
            FilterOperator.LessThanOrEqual => "[lte]",
            FilterOperator.GreaterThan => "[gt]",
            FilterOperator.GreaterThanOrEqual => "[gte]",
            FilterOperator.Range => "[range]",
            FilterOperator.In => "[in]",
            FilterOperator.NotIn => "[nin]",
            FilterOperator.Contains => "[contains]",
            FilterOperator.Any => "[any]",
            FilterOperator.All => "[all]",
            FilterOperator.Empty => "[empty]",
            FilterOperator.NotEmpty => "[nempty]",
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };

        // Empty operators don't need values
        if (filter.Operator is FilterOperator.Empty or FilterOperator.NotEmpty)
        {
            return new KeyValuePair<string, string>(filter.PropertyPath, operatorSuffix);
        }

        var serializedValue = filter.Value?.Serialize() ?? throw new InvalidOperationException(
            $"Filter value cannot be null for operator {filter.Operator}.");

        return new KeyValuePair<string, string>($"{filter.PropertyPath}{operatorSuffix}", serializedValue);
    }
}


