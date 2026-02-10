using System.Collections;

namespace Kontent.Ai.Delivery.Api.Filtering;

/// <summary>
/// Mutable ordered collection of serialized filter key/value pairs.
/// Preserves duplicates and insertion order.
/// </summary>
internal sealed class SerializedFilterCollection : ICollection<KeyValuePair<string, string>>, IReadOnlyList<KeyValuePair<string, string>>
{
    private readonly List<KeyValuePair<string, string>> _filters = [];

    public int Count => _filters.Count;
    public bool IsReadOnly => false;
    public KeyValuePair<string, string> this[int index] => _filters[index];

    public void Add(KeyValuePair<string, string> item) => _filters.Add(item);

    internal void Add(string key, string value) => _filters.Add(new KeyValuePair<string, string>(key, value));

    internal void CopyFrom(SerializedFilterCollection other)
    {
        ArgumentNullException.ThrowIfNull(other);
        _filters.AddRange(other._filters);
    }

    internal SerializedFilterCollection Clone()
    {
        var clone = new SerializedFilterCollection();
        clone._filters.AddRange(_filters);
        return clone;
    }

    internal Dictionary<string, string[]>? ToQueryDictionary() => ToQueryDictionary(this);

    internal static Dictionary<string, string[]>? ToQueryDictionary(IReadOnlyList<KeyValuePair<string, string>> filters)
        => filters.Count == 0
            ? null
            : filters
                .GroupBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Value).ToArray(),
                    StringComparer.OrdinalIgnoreCase);

    public void Clear() => _filters.Clear();

    public bool Contains(KeyValuePair<string, string> item) => _filters.Contains(item);

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => _filters.CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<string, string> item) => _filters.Remove(item);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _filters.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
