namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Maps OneOf discriminated union types to concrete FilterValue implementations.
/// </summary>
internal static class FilterValueMapper
{
    /// <summary>
    /// Maps a scalar value (string, double, DateTime, or bool) to its corresponding FilterValue type.
    /// </summary>
    public static IFilterValue From(ScalarValue s) => s.Match<IFilterValue>(
        StringValue.From,
        NumericValue.From,
        DateTimeValue.From,
        BooleanValue.From
    );

    /// <summary>
    /// Maps range bounds (numeric or date tuple) to its corresponding FilterValue type.
    /// </summary>
    public static IFilterValue From(RangeBounds r) => r.Match<IFilterValue>(
        NumericRangeValue.From,
        DateRangeValue.From
    );

    /// <summary>
    /// Maps a comparable value (double, DateTime, or string) to its corresponding FilterValue type.
    /// </summary>
    public static IFilterValue From(ComparableValue c) => c.Match<IFilterValue>(
        NumericValue.From,
        DateTimeValue.From,
        StringValue.From
    );
}