using Kontent.Ai.Delivery.ContentItems.Mapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Executes requests against the Kontent.ai Delivery API using query builders.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving content of the specified environment.
/// </remarks>
/// <param name="deliveryApi">The Refit-generated API client.</param>
/// <param name="deliveryOptions">The settings of the Kontent.ai environment.</param>
/// <param name="contentItemMapper">The content item mapper for element hydration.</param>
/// <param name="contentDeserializer">The content deserializer for JSON to object conversion.</param>
/// <param name="typeProvider">The type provider for content type to CLR type mapping.</param>
/// <param name="cacheManager">Optional cache manager for caching API responses (injected when EnableCaching is true).</param>
/// <param name="logger">Optional logger for diagnostic output.</param>
internal sealed class DeliveryClient(
    IDeliveryApi deliveryApi,
    IOptionsMonitor<DeliveryOptions> deliveryOptions,
    ContentItemMapper contentItemMapper,
    IContentDeserializer contentDeserializer,
    ITypeProvider typeProvider,
    IDeliveryCacheManager? cacheManager = null,
    ILogger<DeliveryClient>? logger = null) : IDeliveryClient
{
    private readonly IDeliveryApi _deliveryApi = deliveryApi ?? throw new ArgumentNullException(nameof(deliveryApi));
    private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper ?? throw new ArgumentNullException(nameof(contentItemMapper));
    private readonly IContentDeserializer _contentDeserializer = contentDeserializer ?? throw new ArgumentNullException(nameof(contentDeserializer));
    private readonly ITypeProvider _typeProvider = typeProvider ?? throw new ArgumentNullException(nameof(typeProvider));
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger<DeliveryClient>? _logger = logger;

    public IItemQuery<T> GetItem<T>(string codename)
    {
        return string.IsNullOrEmpty(codename)
            ? throw new ArgumentException("Entered item codename is not valid.", nameof(codename))
            : (IItemQuery<T>)new ItemQuery<T>(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent, _contentItemMapper, _contentDeserializer, _cacheManager, _logger);
    }

    public IDynamicItemQuery GetItem(string codename)
    {
        return string.IsNullOrEmpty(codename)
            ? throw new ArgumentException("Entered item codename is not valid.", nameof(codename))
            : (IDynamicItemQuery)new DynamicItemQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent, _contentItemMapper);
    }

    public IItemsQuery<T> GetItems<T>()
    {
        return new ItemsQuery<T>(_deliveryApi, GetDefaultWaitForLoadingNewContent, _contentItemMapper, _contentDeserializer, _typeProvider, _cacheManager, _logger);
    }

    public IDynamicItemsQuery GetItems()
    {
        return new DynamicItemsQuery(_deliveryApi, GetDefaultWaitForLoadingNewContent, _contentItemMapper);
    }

    public IEnumerateItemsQuery<T> GetItemsFeed<T>()
    {
        return new EnumerateItemsQuery<T>(_deliveryApi, GetDefaultWaitForLoadingNewContent, _contentItemMapper, _typeProvider, _logger);
    }

    public IDynamicEnumerateItemsQuery GetItemsFeed()
    {
        return new DynamicEnumerateItemsQuery(_deliveryApi, GetDefaultWaitForLoadingNewContent, _contentItemMapper, _logger);
    }

    public ITypeQuery GetType(string codename)
    {
        return string.IsNullOrEmpty(codename)
            ? throw new ArgumentException("Entered type codename is not valid.", nameof(codename))
            : (ITypeQuery)new TypeQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent, _cacheManager, _logger);
    }

    public ITypesQuery GetTypes()
    {
        return new TypesQuery(_deliveryApi, GetDefaultWaitForLoadingNewContent, _cacheManager, _logger);
    }

    public ITypeElementQuery GetContentElement(string contentTypeCodename, string contentElementCodename)
    {
        if (string.IsNullOrEmpty(contentTypeCodename))
        {
            throw new ArgumentException("Entered content type codename is not valid.", nameof(contentTypeCodename));
        }

        return string.IsNullOrEmpty(contentElementCodename)
            ? throw new ArgumentException("Entered content element codename is not valid.", nameof(contentElementCodename))
            : (ITypeElementQuery)new TypeElementQuery(_deliveryApi, contentTypeCodename, contentElementCodename, GetDefaultWaitForLoadingNewContent);
    }

    public ITaxonomyQuery GetTaxonomy(string codename)
    {
        return string.IsNullOrEmpty(codename)
            ? throw new ArgumentException("Entered taxonomy codename is not valid.", nameof(codename))
            : (ITaxonomyQuery)new TaxonomyQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent, _cacheManager);
    }

    public ITaxonomiesQuery GetTaxonomies()
    {
        return new TaxonomiesQuery(_deliveryApi, GetDefaultWaitForLoadingNewContent, _cacheManager);
    }

    public ILanguagesQuery GetLanguages()
    {
        return new LanguagesQuery(_deliveryApi, GetDefaultWaitForLoadingNewContent);
    }

    public IItemUsedInQuery GetItemUsedIn(string codename)
    {
        return string.IsNullOrEmpty(codename)
            ? throw new ArgumentException("Entered item codename is not valid.", nameof(codename))
            : (IItemUsedInQuery)new ItemUsedInQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent);
    }

    public IAssetUsedInQuery GetAssetUsedIn(string codename)
    {
        return string.IsNullOrEmpty(codename)
            ? throw new ArgumentException("Entered asset codename is not valid.", nameof(codename))
            : (IAssetUsedInQuery)new AssetUsedInQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent);
    }

    private bool? GetDefaultWaitForLoadingNewContent()
        => _deliveryOptions.CurrentValue.WaitForLoadingNewContent ? true : null;
}
