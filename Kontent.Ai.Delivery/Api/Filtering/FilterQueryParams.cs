namespace Kontent.Ai.Delivery.Api.Filtering;

/// <summary>
/// Helpers for converting SDK filter pairs into query parameters suitable for Refit.
/// </summary>
internal static class FilterQueryParams
{
    /// <summary>
    /// Converts a sequence of (key,value) filter pairs into a dictionary of key -> values,
    /// preserving duplicates (including duplicate key/value pairs) and relying on Refit's
    /// CollectionFormat.Multi to serialize repeated keys.
    /// 
    /// This is to ensure duplicate filters are serialized as separate query parameters.
    /// </summary>
    internal static Dictionary<string, string[]>? ToQueryDictionary(IReadOnlyList<KeyValuePair<string, string>> filters)
    {
        if (filters.Count == 0)
            return null;

        return filters
            .GroupBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}

