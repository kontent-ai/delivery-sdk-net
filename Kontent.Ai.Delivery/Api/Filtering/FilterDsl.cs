using System.Globalization;
using System.Text.Encodings.Web;

namespace Kontent.Ai.Delivery.Api.Filtering;

internal static class FilterPath
{
    internal static string System(string propertyName) => Build("system", propertyName);
    internal static string Element(string elementCodename) => Build("elements", elementCodename);

    private static string Build(string prefix, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Property name cannot be null or whitespace.", nameof(name));
        }

        var trimmed = name.Trim();

        if (trimmed.Contains(' '))
        {
            throw new ArgumentException($"Property name '{name}' contains spaces.", nameof(name));
        }

        var expectedPrefix = prefix + ".";
        if (trimmed.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        // Avoid accepting dotted input with the wrong prefix (e.g. System("elements.title")).
        if (trimmed.Contains('.', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Property name '{name}' must be provided without a prefix. Use '{prefix}.' prefix only.",
                nameof(name));
        }

        return $"{prefix}.{trimmed}";
    }
}

internal static class FilterValueSerializer
{
    internal static string Serialize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return UrlEncoder.Default.Encode(value);
    }

    internal static string Serialize(bool value) => value.ToString().ToLowerInvariant();

    internal static string Serialize(double value) => value.ToString(CultureInfo.InvariantCulture);

    internal static string Serialize(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    internal static string SerializeRange(double lower, double upper)
    {
        if (lower > upper)
        {
            throw new ArgumentException($"Invalid range: lower bound ({lower}) cannot be greater than upper bound ({upper}).");
        }
        return $"{Serialize(lower)},{Serialize(upper)}";
    }

    internal static string SerializeRange(DateTime lower, DateTime upper)
    {
        // Normalize before comparison to avoid surprising Local/Unspecified ordering.
        var l = DateTime.Parse(Serialize(lower), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        var u = DateTime.Parse(Serialize(upper), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        if (l > u)
        {
            throw new ArgumentException($"Invalid date range: lower bound ({lower:yyyy-MM-dd HH:mm:ss}) cannot be after upper bound ({upper:yyyy-MM-dd HH:mm:ss}).");
        }
        return $"{Serialize(lower)},{Serialize(upper)}";
    }

    internal static string SerializeRange(string lower, string upper)
    {
        ArgumentNullException.ThrowIfNull(lower);
        ArgumentNullException.ThrowIfNull(upper);

        if (string.Compare(lower, upper, StringComparison.Ordinal) > 0)
        {
            throw new ArgumentException(
                $"Invalid string range: lower bound (\"{lower}\") is lexicographically after upper bound (\"{upper}\").",
                nameof(lower));
        }

        return $"{Serialize(lower)},{Serialize(upper)}";
    }

    internal static string SerializeArray(string[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0) throw new ArgumentException("Array cannot be empty. Provide at least one value.", nameof(values));
        return string.Join(",", values.Select(Serialize));
    }

    internal static string SerializeArray(double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0) throw new ArgumentException("Array cannot be empty. Provide at least one value.", nameof(values));
        return string.Join(",", values.Select(Serialize));
    }

    internal static string SerializeArray(DateTime[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0) throw new ArgumentException("Array cannot be empty. Provide at least one value.", nameof(values));
        return string.Join(",", values.Select(Serialize));
    }
}

internal static class FilterSuffix
{
    internal const string Eq = "[eq]";
    internal const string Neq = "[neq]";
    internal const string Lt = "[lt]";
    internal const string Lte = "[lte]";
    internal const string Gt = "[gt]";
    internal const string Gte = "[gte]";
    internal const string Range = "[range]";
    internal const string In = "[in]";
    internal const string Nin = "[nin]";
    internal const string Contains = "[contains]";
    internal const string Any = "[any]";
    internal const string All = "[all]";
    internal const string Empty = "[empty]";
    internal const string Nempty = "[nempty]";
}

internal readonly struct ItemFieldFilter<TBuilder>(TBuilder builder, string path, Action<KeyValuePair<string, string>> add)
    : IItemFieldFilter<TBuilder>
{
    public TBuilder IsEqualTo(string value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(double value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(DateTime value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(bool value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));

    public TBuilder IsNotEqualTo(string value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(double value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(DateTime value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(bool value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));

    public TBuilder IsLessThan(double value) => AddValue(FilterSuffix.Lt, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThan(DateTime value) => AddValue(FilterSuffix.Lt, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThan(string value) => AddValue(FilterSuffix.Lt, FilterValueSerializer.Serialize(value));

    public TBuilder IsLessThanOrEqualTo(double value) => AddValue(FilterSuffix.Lte, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThanOrEqualTo(DateTime value) => AddValue(FilterSuffix.Lte, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThanOrEqualTo(string value) => AddValue(FilterSuffix.Lte, FilterValueSerializer.Serialize(value));

    public TBuilder IsGreaterThan(double value) => AddValue(FilterSuffix.Gt, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThan(DateTime value) => AddValue(FilterSuffix.Gt, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThan(string value) => AddValue(FilterSuffix.Gt, FilterValueSerializer.Serialize(value));

    public TBuilder IsGreaterThanOrEqualTo(double value) => AddValue(FilterSuffix.Gte, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThanOrEqualTo(DateTime value) => AddValue(FilterSuffix.Gte, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThanOrEqualTo(string value) => AddValue(FilterSuffix.Gte, FilterValueSerializer.Serialize(value));

    public TBuilder IsWithinRange(double lower, double upper) => AddValue(FilterSuffix.Range, FilterValueSerializer.SerializeRange(lower, upper));
    public TBuilder IsWithinRange(DateTime lower, DateTime upper) => AddValue(FilterSuffix.Range, FilterValueSerializer.SerializeRange(lower, upper));
    public TBuilder IsWithinRange(string lower, string upper) => AddValue(FilterSuffix.Range, FilterValueSerializer.SerializeRange(lower, upper));

    public TBuilder IsIn(params string[] values) => AddValue(FilterSuffix.In, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsIn(params double[] values) => AddValue(FilterSuffix.In, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsIn(params DateTime[] values) => AddValue(FilterSuffix.In, FilterValueSerializer.SerializeArray(values));

    public TBuilder IsNotIn(params string[] values) => AddValue(FilterSuffix.Nin, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsNotIn(params double[] values) => AddValue(FilterSuffix.Nin, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsNotIn(params DateTime[] values) => AddValue(FilterSuffix.Nin, FilterValueSerializer.SerializeArray(values));

    public TBuilder Contains(string value) => AddValue(FilterSuffix.Contains, FilterValueSerializer.Serialize(value));
    public TBuilder ContainsAny(params string[] values) => AddValue(FilterSuffix.Any, FilterValueSerializer.SerializeArray(values));
    public TBuilder ContainsAll(params string[] values) => AddValue(FilterSuffix.All, FilterValueSerializer.SerializeArray(values));

    public TBuilder IsEmpty()
    {
        add(new KeyValuePair<string, string>(path, FilterSuffix.Empty));
        return builder;
    }

    public TBuilder IsNotEmpty()
    {
        add(new KeyValuePair<string, string>(path, FilterSuffix.Nempty));
        return builder;
    }

    private TBuilder AddValue(string suffix, string value)
    {
        add(new KeyValuePair<string, string>(path + suffix, value));
        return builder;
    }
}

internal readonly struct TypeFieldFilter<TBuilder>(TBuilder builder, string path, Action<KeyValuePair<string, string>> add)
    : ITypeFieldFilter<TBuilder>
{
    public TBuilder IsEqualTo(string value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(DateTime value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));

    public TBuilder IsNotEqualTo(string value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(DateTime value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));

    public TBuilder IsIn(params string[] values) => AddValue(FilterSuffix.In, FilterValueSerializer.SerializeArray(values));
    public TBuilder IsNotIn(params string[] values) => AddValue(FilterSuffix.Nin, FilterValueSerializer.SerializeArray(values));

    public TBuilder IsWithinRange(DateTime lower, DateTime upper) => AddValue(FilterSuffix.Range, FilterValueSerializer.SerializeRange(lower, upper));
    public TBuilder IsLessThan(DateTime value) => AddValue(FilterSuffix.Lt, FilterValueSerializer.Serialize(value));
    public TBuilder IsLessThanOrEqualTo(DateTime value) => AddValue(FilterSuffix.Lte, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThan(DateTime value) => AddValue(FilterSuffix.Gt, FilterValueSerializer.Serialize(value));
    public TBuilder IsGreaterThanOrEqualTo(DateTime value) => AddValue(FilterSuffix.Gte, FilterValueSerializer.Serialize(value));

    private TBuilder AddValue(string suffix, string value)
    {
        add(new KeyValuePair<string, string>(path + suffix, value));
        return builder;
    }
}

internal readonly struct TaxonomyFieldFilter<TBuilder>(TBuilder builder, string path, Action<KeyValuePair<string, string>> add)
    : ITaxonomyFieldFilter<TBuilder>
{
    public TBuilder IsEqualTo(string value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));
    public TBuilder IsEqualTo(DateTime value) => AddValue(FilterSuffix.Eq, FilterValueSerializer.Serialize(value));

    public TBuilder IsNotEqualTo(string value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));
    public TBuilder IsNotEqualTo(DateTime value) => AddValue(FilterSuffix.Neq, FilterValueSerializer.Serialize(value));

    private TBuilder AddValue(string suffix, string value)
    {
        add(new KeyValuePair<string, string>(path + suffix, value));
        return builder;
    }
}

internal sealed class ItemsFilterBuilder(ICollection<KeyValuePair<string, string>> filters) : IItemsFilterBuilder
{
    public IItemFieldFilter<IItemsFilterBuilder> System(string propertyName)
        => new ItemFieldFilter<IItemsFilterBuilder>(this, FilterPath.System(propertyName), filters.Add);

    public IItemFieldFilter<IItemsFilterBuilder> Element(string elementCodename)
        => new ItemFieldFilter<IItemsFilterBuilder>(this, FilterPath.Element(elementCodename), filters.Add);
}

internal sealed class TypesFilterBuilder(ICollection<KeyValuePair<string, string>> filters) : ITypesFilterBuilder
{
    public ITypeFieldFilter<ITypesFilterBuilder> System(string propertyName)
        => new TypeFieldFilter<ITypesFilterBuilder>(this, FilterPath.System(propertyName), filters.Add);
}

internal sealed class TaxonomiesFilterBuilder(ICollection<KeyValuePair<string, string>> filters) : ITaxonomiesFilterBuilder
{
    public ITaxonomyFieldFilter<ITaxonomiesFilterBuilder> System(string propertyName)
        => new TaxonomyFieldFilter<ITaxonomiesFilterBuilder>(this, FilterPath.System(propertyName), filters.Add);
}


