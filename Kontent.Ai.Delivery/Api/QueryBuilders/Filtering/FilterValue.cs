using System.Globalization;
using System.Text.Encodings.Web;
using ValueOf;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering
{
    /// <summary>
    /// Base class for all filter values.
    /// </summary>
    public abstract class FilterValue<T, TSelf>
        : ValueOf<T, TSelf>, IFilterValue
        where TSelf : FilterValue<T, TSelf>, new()
    {
        /// <summary>
        /// Serializes the filter value to a string format suitable for the API.
        /// </summary>
        protected abstract string SerializeInternal();

        string IFilterValue.Serialize() => SerializeInternal();
    }

    /// <summary>
    /// Represents an empty filter value.
    /// </summary>
    public sealed class EmptyValue : FilterValue<string, EmptyValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() => "";
    }

    /// <summary>
    /// Represents a single string value.
    /// </summary>
    public sealed class StringValue : FilterValue<string, StringValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            UrlEncoder.Default.Encode(Value);
    }

    /// <summary>
    /// Represents a numeric (double) value.
    /// </summary>
    public sealed class NumericValue : FilterValue<double, NumericValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Represents a DateTime value.
    /// </summary>
    public sealed class DateTimeValue : FilterValue<DateTime, DateTimeValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Represents a Boolean value.
    /// </summary>
    public sealed class BooleanValue : FilterValue<bool, BooleanValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            Value.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Represents an array of strings.
    /// </summary>
    public sealed class StringArrayValue : FilterValue<string[], StringArrayValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            string.Join(",", Value.Select(UrlEncoder.Default.Encode));
    }

    /// <summary>
    /// Represents an array of doubles.
    /// </summary>
    public sealed class NumericArrayValue : FilterValue<double[], NumericArrayValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            string.Join(",", Value.Select(v => v.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Represents an array of DateTime values.
    /// </summary>
    public sealed class DateTimeArrayValue : FilterValue<DateTime[], DateTimeArrayValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            string.Join(",", Value.Select(v => v.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Represents a numeric range with lower and upper bounds.
    /// </summary>
    public sealed class NumericRangeValue : FilterValue<(double Lower, double Upper), NumericRangeValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            $"{Value.Lower.ToString(CultureInfo.InvariantCulture)},{Value.Upper.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Represents a date range with lower and upper bounds.
    /// </summary>
    public sealed class DateRangeValue : FilterValue<(DateTime Lower, DateTime Upper), DateRangeValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            $"{Value.Lower:yyyy-MM-ddTHH:mm:ssZ},{Value.Upper:yyyy-MM-ddTHH:mm:ssZ}";
    }

    /// <summary>
    /// Represents a string range with lower and upper bounds.
    /// </summary>
    public sealed class StringRangeValue : FilterValue<(string Lower, string Upper), StringRangeValue>
    {
        /// <inheritdoc />
        protected override string SerializeInternal() =>
            $"{UrlEncoder.Default.Encode(Value.Lower)},{UrlEncoder.Default.Encode(Value.Upper)}";
    }
}
