using System;
using System.Collections.Concurrent;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Default implementation of <see cref="IItemTypingStrategy"/> that uses an <see cref="ITypeProvider"/>
/// to resolve model types, falling back to <see cref="DynamicElements"/> when no mapping exists.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="DefaultItemTypingStrategy"/>.
/// </remarks>
/// <param name="typeProvider">The type provider to use for resolving model types.</param>
internal sealed class DefaultItemTypingStrategy(ITypeProvider typeProvider) : IItemTypingStrategy
{
    private readonly ITypeProvider _typeProvider = typeProvider ?? throw new ArgumentNullException(nameof(typeProvider));
    private readonly ConcurrentDictionary<string, Type> _cache = new();

    /// <summary>
    /// Resolves the model type for the given content type codename.
    /// Uses cached results for repeated lookups.
    /// </summary>
    /// <param name="contentTypeCodename">The content type codename.</param>
    /// <returns>The resolved model type, or <see cref="DynamicElements"/> if no mapping exists.</returns>
    public Type ResolveModelType(string contentTypeCodename)
    {
        if (string.IsNullOrEmpty(contentTypeCodename))
        {
            return typeof(DynamicElements);
        }

        return _cache.GetOrAdd(contentTypeCodename, codename =>
            _typeProvider.TryGetModelType(codename) ?? typeof(DynamicElements));
    }
}
