using System;
using System.Linq;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// Represents a filter value that can be a single value, multiple values, a range, or empty.
/// </summary>
public abstract class FilterValue
{
    /// <summary>
    /// Serializes this filter value to a string format suitable for the API.
    /// </summary>
    /// <returns>The serialized value string.</returns>
    public abstract string Serialize();

    /// <summary>
    /// Represents an empty filter value for operators that don't require values.
    /// </summary>
    public sealed class Empty : FilterValue
    {
        internal Empty() { }
        public override string Serialize() => "";
    }

    /// <summary>
    /// Represents a single string value.
    /// </summary>
    public sealed class StringValue : FilterValue
    {
        public string Value { get; }
        internal StringValue(string value) => Value = value;
        public override string Serialize() => System.Text.Encodings.Web.UrlEncoder.Default.Encode(Value);
    }

    /// <summary>
    /// Represents a single integer value.
    /// </summary>
    public sealed class IntValue : FilterValue
    {
        public int Value { get; }
        internal IntValue(int value) => Value = value;
        public override string Serialize() => Value.ToString();
    }

    /// <summary>
    /// Represents a single DateTime value.
    /// </summary>
    public sealed class DateTimeValue : FilterValue
    {
        public DateTime Value { get; }
        internal DateTimeValue(DateTime value) => Value = value;
        public override string Serialize() => Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    /// <summary>
    /// Represents a single boolean value.
    /// </summary>
    public sealed class BooleanValue : FilterValue
    {
        public bool Value { get; }
        internal BooleanValue(bool value) => Value = value;
        public override string Serialize() => Value.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Represents multiple string values.
    /// </summary>
    public sealed class StringArrayValue : FilterValue
    {
        public string[] Value { get; }
        internal StringArrayValue(string[] value) => Value = value;
        public override string Serialize() => string.Join(",", Value.Select(System.Text.Encodings.Web.UrlEncoder.Default.Encode));
    }

    /// <summary>
    /// Represents multiple integer values.
    /// </summary>
    public sealed class IntArrayValue : FilterValue
    {
        public int[] Value { get; }
        internal IntArrayValue(int[] value) => Value = value;
        public override string Serialize() => string.Join(",", Value.Select(v => v.ToString()));
    }

    /// <summary>
    /// Represents multiple DateTime values.
    /// </summary>
    public sealed class DateTimeArrayValue : FilterValue
    {
        public DateTime[] Value { get; }
        internal DateTimeArrayValue(DateTime[] value) => Value = value;
        public override string Serialize() => string.Join(",", Value.Select(v => v.ToString("yyyy-MM-ddTHH:mm:ssZ")));
    }

    /// <summary>
    /// Represents a string range with lower and upper bounds.
    /// </summary>
    public sealed class StringRange : FilterValue
    {
        public string Lower { get; }
        public string Upper { get; }
        internal StringRange(string lower, string upper) => (Lower, Upper) = (lower, upper);
        public override string Serialize() => $"{System.Text.Encodings.Web.UrlEncoder.Default.Encode(Lower)},{System.Text.Encodings.Web.UrlEncoder.Default.Encode(Upper)}";
    }

    /// <summary>
    /// Represents a numeric range with lower and upper bounds.
    /// </summary>
    public sealed class NumericRange : FilterValue
    {
        public int Lower { get; }
        public int Upper { get; }
        internal NumericRange(int lower, int upper) => (Lower, Upper) = (lower, upper);
        public override string Serialize() => $"{Lower},{Upper}";
    }

    /// <summary>
    /// Represents a date range with lower and upper bounds.
    /// </summary>
    public sealed class DateRange : FilterValue
    {
        public DateTime Lower { get; }
        public DateTime Upper { get; }
        internal DateRange(DateTime lower, DateTime upper) => (Lower, Upper) = (lower, upper);
        public override string Serialize() => $"{Lower:yyyy-MM-ddTHH:mm:ssZ},{Upper:yyyy-MM-ddTHH:mm:ssZ}";
    }

    /// <summary>
    /// Empty filter value for operators that don't require values.
    /// </summary>
    public static readonly FilterValue EmptyValue = new Empty();

    // Implicit conversion operators for ease of use
    public static implicit operator FilterValue(string value) => new StringValue(value);
    public static implicit operator FilterValue(int value) => new IntValue(value);
    public static implicit operator FilterValue(DateTime value) => new DateTimeValue(value);
    public static implicit operator FilterValue(bool value) => new BooleanValue(value);
    public static implicit operator FilterValue(string[] values) => new StringArrayValue(values);
    public static implicit operator FilterValue(int[] values) => new IntArrayValue(values);
    public static implicit operator FilterValue(DateTime[] values) => new DateTimeArrayValue(values);
}