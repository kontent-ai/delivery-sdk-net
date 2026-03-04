namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Specifies how the cache manager stores Delivery API responses.
/// </summary>
public enum CacheStorageMode
{
    /// <summary>
    /// Stores fully hydrated C# objects. Suitable for in-memory caches (e.g., <c>IMemoryCache</c>)
    /// where object references are preserved directly.
    /// </summary>
    HydratedObject = 0,

    /// <summary>
    /// Stores raw JSON strings extracted from the API response. Suitable for hybrid/distributed caches
    /// (e.g., Redis, SQL Server) where values must be serialized. Avoids serialization issues with
    /// complex object graphs (circular references, custom converters, non-serializable types).
    /// On cache hit, the raw JSON is rehydrated using the SDK's standard deserialization pipeline.
    /// </summary>
    RawJson = 1
}
