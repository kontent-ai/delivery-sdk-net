

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

internal static class FilterValueMapper
{
    public static IFilterValue From(Scalar s) => s.Match<IFilterValue>(
        StringValue.From,
        NumericValue.From,
        DateTimeValue.From,
        BooleanValue.From
    );

    public static IFilterValue From(RangeTuple r) => r.Match<IFilterValue>(
        NumericRangeValue.From,
        DateRangeValue.From
    );

    public static IFilterValue From(Comparable c) => c.Match<IFilterValue>(
        NumericValue.From,
        DateTimeValue.From,
        StringValue.From
    );
}
