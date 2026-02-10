using System.Globalization;

namespace Kontent.Ai.Delivery.Api.Filtering;

internal static class FilterValueSerializer
{
    internal static string Serialize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        // Encoding is handled by Refit.
        return value;
    }

    internal static string Serialize(bool value) => value.ToString().ToLowerInvariant();

    internal static string Serialize(double value) => value.ToString(CultureInfo.InvariantCulture);

    internal static string Serialize(DateTime value)
    {
        var utc = NormalizeToUtc(value);
        return utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    internal static string SerializeRange(double lower, double upper)
    {
        return lower > upper
            ? throw new ArgumentException($"Invalid range: lower bound ({lower}) cannot be greater than upper bound ({upper}).")
            : $"{Serialize(lower)},{Serialize(upper)}";
    }

    internal static string SerializeRange(DateTime lower, DateTime upper)
    {
        var normalizedLower = NormalizeForRangeComparison(lower);
        var normalizedUpper = NormalizeForRangeComparison(upper);

        return normalizedLower > normalizedUpper
            ? throw new ArgumentException($"Invalid date range: lower bound ({lower:yyyy-MM-dd HH:mm:ss}) cannot be after upper bound ({upper:yyyy-MM-dd HH:mm:ss}).")
            : $"{Serialize(lower)},{Serialize(upper)}";
    }

    internal static string SerializeRange(string lower, string upper)
    {
        ArgumentNullException.ThrowIfNull(lower);
        ArgumentNullException.ThrowIfNull(upper);

        return string.Compare(lower, upper, StringComparison.Ordinal) > 0
            ? throw new ArgumentException(
                $"Invalid string range: lower bound (\"{lower}\") is lexicographically after upper bound (\"{upper}\").",
                nameof(lower))
            : $"{Serialize(lower)},{Serialize(upper)}";
    }

    internal static string SerializeArray(string[] values) => SerializeArray(values, Serialize);

    internal static string SerializeArray(double[] values) => SerializeArray(values, Serialize);

    internal static string SerializeArray(DateTime[] values) => SerializeArray(values, Serialize);

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            // DateTimeKind.Unspecified is treated as UTC (no offset conversion).
            // Prefer passing DateTime values with Kind=Utc or use DateTimeOffset and convert explicitly.
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static DateTime NormalizeForRangeComparison(DateTime value)
    {
        var utc = NormalizeToUtc(value);
        return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, DateTimeKind.Utc);
    }

    private static string SerializeArray<T>(T[] values, Func<T, string> serializer)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Length == 0
            ? throw new ArgumentException("Array cannot be empty. Provide at least one value.", nameof(values))
            : string.Join(",", values.Select(serializer));
    }
}
