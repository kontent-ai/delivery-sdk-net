using System.Globalization;
using System.Text.Encodings.Web;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Represents a single string value.
/// </summary>
public sealed record StringValue : IFilterValue
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a string filter value.
    /// </summary>
    /// <param name="value">The string value (cannot be null).</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public StringValue(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value), "String value cannot be null.");
    }

    /// <summary>
    /// Creates a string filter value.
    /// </summary>
    public static StringValue From(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="StringValue"/> to a <see cref="string"/>.
    /// </summary>
    public static implicit operator string(StringValue value) => value.Value;

    /// <inheritdoc />
    public string Serialize() => UrlEncoder.Default.Encode(Value);
}

/// <summary>
/// Represents a numeric (double) value.
/// </summary>
public sealed record NumericValue(double Value) : IFilterValue
{
    /// <summary>
    /// Creates a numeric filter value.
    /// </summary>
    public static NumericValue From(double value) => new(value);

    /// <inheritdoc />
    public string Serialize() => Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// Represents a DateTime value.
/// </summary>
public sealed record DateTimeValue(DateTime Value) : IFilterValue
{
    /// <summary>
    /// Creates a DateTime filter value.
    /// </summary>
    public static DateTimeValue From(DateTime value) => new(value);

    /// <inheritdoc />
    public string Serialize() => Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}

/// <summary>
/// Represents a Boolean value.
/// </summary>
public sealed record BooleanValue(bool Value) : IFilterValue
{
    /// <summary>
    /// Creates a boolean filter value.
    /// </summary>
    public static BooleanValue From(bool value) => new(value);

    /// <inheritdoc />
    public string Serialize() => Value.ToString().ToLowerInvariant();
}

/// <summary>
/// Represents an array of strings.
/// </summary>
public sealed record StringArrayValue : IFilterValue
{
    /// <summary>
    /// Gets the string array value.
    /// </summary>
    public string[] Value { get; }

    /// <summary>
    /// Creates a string array filter value.
    /// </summary>
    /// <param name="value">The string array (cannot be null or empty).</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when array is empty.</exception>
    public StringArrayValue(string[] value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "String array cannot be null.");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("String array cannot be empty. Provide at least one value.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Creates a string array filter value.
    /// </summary>
    public static StringArrayValue From(string[] value) => new(value);

    /// <inheritdoc />
    public string Serialize() => string.Join(",", Value.Select(UrlEncoder.Default.Encode));

    /// <inheritdoc />
    public bool Equals(StringArrayValue? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.SequenceEqual(other.Value);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Value)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}

/// <summary>
/// Represents an array of doubles.
/// </summary>
public sealed record NumericArrayValue : IFilterValue
{
    /// <summary>
    /// Gets the numeric array value.
    /// </summary>
    public double[] Value { get; }

    /// <summary>
    /// Creates a numeric array filter value.
    /// </summary>
    /// <param name="value">The numeric array (cannot be null or empty).</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when array is empty.</exception>
    public NumericArrayValue(double[] value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "Numeric array cannot be null.");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("Numeric array cannot be empty. Provide at least one value.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Creates a numeric array filter value.
    /// </summary>
    public static NumericArrayValue From(double[] value) => new(value);

    /// <inheritdoc />
    public string Serialize() => string.Join(",", Value.Select(v => v.ToString(CultureInfo.InvariantCulture)));

    /// <inheritdoc />
    public bool Equals(NumericArrayValue? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.SequenceEqual(other.Value);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Value)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}

/// <summary>
/// Represents an array of DateTime values.
/// </summary>
public sealed record DateTimeArrayValue : IFilterValue
{
    /// <summary>
    /// Gets the DateTime array value.
    /// </summary>
    public DateTime[] Value { get; }

    /// <summary>
    /// Creates a DateTime array filter value.
    /// </summary>
    /// <param name="value">The DateTime array (cannot be null or empty).</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when array is empty.</exception>
    public DateTimeArrayValue(DateTime[] value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "DateTime array cannot be null.");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("DateTime array cannot be empty. Provide at least one value.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Creates a DateTime array filter value.
    /// </summary>
    public static DateTimeArrayValue From(DateTime[] value) => new(value);

    /// <inheritdoc />
    public string Serialize() => string.Join(",", Value.Select(v => v.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)));

    /// <inheritdoc />
    public bool Equals(DateTimeArrayValue? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.SequenceEqual(other.Value);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Value)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}

/// <summary>
/// Represents a numeric range with lower and upper bounds.
/// </summary>
public sealed record NumericRangeValue : IFilterValue
{
    /// <summary>
    /// Gets the lower bound of the range.
    /// </summary>
    public double Lower { get; }

    /// <summary>
    /// Gets the upper bound of the range.
    /// </summary>
    public double Upper { get; }

    /// <summary>
    /// Creates a numeric range filter value.
    /// </summary>
    /// <param name="lower">The lower bound (inclusive).</param>
    /// <param name="upper">The upper bound (inclusive).</param>
    /// <exception cref="ArgumentException">Thrown when lower bound is greater than upper bound.</exception>
    public NumericRangeValue(double lower, double upper)
    {
        if (lower > upper)
        {
            throw new ArgumentException(
                $"Invalid range: lower bound ({lower}) cannot be greater than upper bound ({upper}).",
                nameof(lower));
        }

        Lower = lower;
        Upper = upper;
    }

    /// <summary>
    /// Creates a numeric range filter value.
    /// </summary>
    public static NumericRangeValue From((double Lower, double Upper) range) => new(range.Lower, range.Upper);

    /// <inheritdoc />
    public string Serialize() => $"{Lower.ToString(CultureInfo.InvariantCulture)},{Upper.ToString(CultureInfo.InvariantCulture)}";
}

/// <summary>
/// Represents a date range with lower and upper bounds.
/// </summary>
public sealed record DateRangeValue : IFilterValue
{
    /// <summary>
    /// Gets the lower bound of the range.
    /// </summary>
    public DateTime Lower { get; }

    /// <summary>
    /// Gets the upper bound of the range.
    /// </summary>
    public DateTime Upper { get; }

    /// <summary>
    /// Creates a date range filter value.
    /// </summary>
    /// <param name="lower">The lower bound (inclusive).</param>
    /// <param name="upper">The upper bound (inclusive).</param>
    /// <exception cref="ArgumentException">Thrown when lower bound is after upper bound.</exception>
    public DateRangeValue(DateTime lower, DateTime upper)
    {
        if (lower > upper)
        {
            throw new ArgumentException(
                $"Invalid date range: lower bound ({lower:yyyy-MM-dd HH:mm:ss}) cannot be after upper bound ({upper:yyyy-MM-dd HH:mm:ss}).",
                nameof(lower));
        }

        Lower = lower;
        Upper = upper;
    }

    /// <summary>
    /// Creates a date range filter value.
    /// </summary>
    public static DateRangeValue From((DateTime Lower, DateTime Upper) range) => new(range.Lower, range.Upper);

    /// <inheritdoc />
    public string Serialize() => $"{Lower:yyyy-MM-ddTHH:mm:ssZ},{Upper:yyyy-MM-ddTHH:mm:ssZ}";
}

/// <summary>
/// Represents a string range with lower and upper bounds.
/// </summary>
public sealed record StringRangeValue : IFilterValue
{
    /// <summary>
    /// Gets the lower bound of the range.
    /// </summary>
    public string Lower { get; }

    /// <summary>
    /// Gets the upper bound of the range.
    /// </summary>
    public string Upper { get; }

    /// <summary>
    /// Creates a string range filter value.
    /// </summary>
    /// <param name="lower">The lower bound (inclusive, cannot be null).</param>
    /// <param name="upper">The upper bound (inclusive, cannot be null).</param>
    /// <exception cref="ArgumentNullException">Thrown when lower or upper is null.</exception>
    /// <exception cref="ArgumentException">Thrown when lower is lexicographically after upper.</exception>
    public StringRangeValue(string lower, string upper)
    {
        if (lower is null)
        {
            throw new ArgumentNullException(nameof(lower), "Lower bound cannot be null.");
        }

        if (upper is null)
        {
            throw new ArgumentNullException(nameof(upper), "Upper bound cannot be null.");
        }

        if (string.Compare(lower, upper, StringComparison.Ordinal) > 0)
        {
            throw new ArgumentException(
                $"Invalid string range: lower bound (\"{lower}\") is lexicographically after upper bound (\"{upper}\").",
                nameof(lower));
        }

        Lower = lower;
        Upper = upper;
    }

    /// <summary>
    /// Creates a string range filter value.
    /// </summary>
    public static StringRangeValue From((string Lower, string Upper) range) => new(range.Lower, range.Upper);

    /// <inheritdoc />
    public string Serialize() => $"{UrlEncoder.Default.Encode(Lower)},{UrlEncoder.Default.Encode(Upper)}";
}