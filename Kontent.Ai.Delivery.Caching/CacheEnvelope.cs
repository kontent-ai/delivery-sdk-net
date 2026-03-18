namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Internal storage envelope that preserves dependency keys alongside the cached value
/// so they survive cache round-trips (including distributed cache serialization).
/// </summary>
internal sealed record CacheEnvelope<T>(T Value, string[] DependencyKeys);
