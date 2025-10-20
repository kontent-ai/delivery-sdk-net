using System.Globalization;
using System.Text.Encodings.Web;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

/// <summary>
/// Represents an empty filter value.
/// </summary>
public sealed record EmptyValue : IFilterValue
{
    private static readonly EmptyValue Instance = new();

    private EmptyValue() { }

    /// <summary>
    /// Creates an empty filter value.
    /// </summary>
    public static EmptyValue From() => Instance;

    /// <inheritdoc />
    public string Serialize() => string.Empty;
}

/// <summary>
/// Represents a single string value.
/// </summary>
public sealed record StringValue(string Value) : IFilterValue
{
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
public sealed record StringArrayValue(string[] Value) : IFilterValue
{
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
public sealed record NumericArrayValue(double[] Value) : IFilterValue
{
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
public sealed record DateTimeArrayValue(DateTime[] Value) : IFilterValue
{
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
public sealed record NumericRangeValue(double Lower, double Upper) : IFilterValue
{
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
public sealed record DateRangeValue(DateTime Lower, DateTime Upper) : IFilterValue
{
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
public sealed record StringRangeValue(string Lower, string Upper) : IFilterValue
{
    /// <summary>
    /// Creates a string range filter value.
    /// </summary>
    public static StringRangeValue From((string Lower, string Upper) range) => new(range.Lower, range.Upper);

    /// <inheritdoc />
    public string Serialize() => $"{UrlEncoder.Default.Encode(Lower)},{UrlEncoder.Default.Encode(Upper)}";
}
