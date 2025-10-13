namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Concrete implementation of IFilter that represents a filtering operation.
/// </summary>
internal sealed record Filter(
    string PropertyPath,
    FilterOperator Operator,
    IFilterValue? Value = null) : IFilter
{
    /// <summary>
    /// Serializes this filter to the Kontent.ai API query parameter format.
    /// </summary>
    /// <returns>The serialized filter string.</returns>
    public KeyValuePair<string, string> ToQueryParameter()
    {
        var operatorSuffix = Operator switch
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
            _ => throw new ArgumentOutOfRangeException(nameof(Operator), Operator, null)
        };

        // Empty operators don't need values
        if (Operator is FilterOperator.Empty or FilterOperator.NotEmpty)
        {
            return new KeyValuePair<string, string>(PropertyPath, operatorSuffix);
        }

        var serializedValue = Value?.Serialize() ?? null;

        return new KeyValuePair<string, string>($"{PropertyPath}{operatorSuffix}", serializedValue);
    }
}