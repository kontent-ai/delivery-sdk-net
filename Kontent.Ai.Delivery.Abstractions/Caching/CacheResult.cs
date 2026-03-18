namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents the result of a cache retrieval, bundling the cached value with its dependency keys.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
/// <param name="Value">The cached value.</param>
/// <param name="DependencyKeys">
/// Canonical dependency keys associated with this cached entry.
/// These keys can be used for downstream cache invalidation scenarios
/// such as ASP.NET output-cache tagging.
/// </param>
public sealed record CacheResult<T>(T Value, IReadOnlyList<string> DependencyKeys) where T : class;
