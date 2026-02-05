using System.Collections.Concurrent;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Default implementation of <see cref="IItemTypingStrategy"/> that uses an <see cref="ITypeProvider"/>
/// to resolve model types, falling back to <see cref="DynamicElements"/> when no mapping exists.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="DefaultItemTypingStrategy"/>.
/// </remarks>
/// <param name="typeProvider">The type provider to use for resolving model types.</param>
/// <param name="logger">Optional logger for diagnostic output.</param>
internal sealed class DefaultItemTypingStrategy(ITypeProvider typeProvider, ILogger<DefaultItemTypingStrategy>? logger = null) : IItemTypingStrategy
{
    private readonly ITypeProvider _typeProvider = typeProvider ?? throw new ArgumentNullException(nameof(typeProvider));
    private readonly ILogger<DefaultItemTypingStrategy>? _logger = logger;
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
            if (_logger is not null)
                LoggerMessages.ContentTypeFallbackToDynamic(_logger, contentTypeCodename ?? "(null)");
            return typeof(DynamicElements);
        }

        return _cache.GetOrAdd(contentTypeCodename, codename =>
        {
            var modelType = _typeProvider.GetType(codename);
            if (modelType is null && _logger is not null)
            {
                LoggerMessages.ContentTypeFallbackToDynamic(_logger, codename);
            }
            return modelType ?? typeof(DynamicElements);
        });
    }
}
