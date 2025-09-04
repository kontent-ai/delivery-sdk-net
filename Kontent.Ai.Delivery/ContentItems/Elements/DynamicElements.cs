using System.Collections;
using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Represents the elements of a content item.
/// </summary>
public sealed record DynamicElements : IElementsModel, IReadOnlyDictionary<string, JsonElement>
{
    private readonly IReadOnlyDictionary<string, JsonElement> _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicElements"/> class.
    /// </summary>
    /// <param name="data">The data to initialize the <see cref="DynamicElements"/> instance with.</param>
    public DynamicElements(IReadOnlyDictionary<string, JsonElement> data)
        => _data = data;

    /// <inheritdoc/>
    public JsonElement this[string key] => _data[key];
    /// <inheritdoc/>
    public IEnumerable<string> Keys => _data.Keys;
    /// <inheritdoc/>
    public IEnumerable<JsonElement> Values => _data.Values;
    /// <inheritdoc/>
    public int Count => _data.Count;
    /// <inheritdoc/>
    public bool ContainsKey(string key) => _data.ContainsKey(key);
    /// <inheritdoc/>
    public bool TryGetValue(string key, out JsonElement value) => _data.TryGetValue(key, out value);
    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, JsonElement>> GetEnumerator() => _data.GetEnumerator();
    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
}
