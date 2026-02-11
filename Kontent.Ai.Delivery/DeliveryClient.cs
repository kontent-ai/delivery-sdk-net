using Kontent.Ai.Delivery.ContentItems.Mapping;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Executes requests against the Kontent.ai Delivery API using query builders.
/// </summary>
internal sealed class DeliveryClient : IDeliveryClient
{
    private readonly IDeliveryApi _deliveryApi;
    private readonly ContentItemMapper _contentItemMapper;
    private readonly IContentDeserializer _contentDeserializer;
    private readonly ITypeProvider _typeProvider;
    private readonly IDeliveryCacheManager? _cacheManager;
    private readonly ILogger<DeliveryClient>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving content of the specified environment.
    /// </summary>
    /// <param name="deliveryApi">The Refit-generated API client.</param>
    /// <param name="contentItemMapper">The content item mapper for element hydration.</param>
    /// <param name="contentDeserializer">The content deserializer for JSON to object conversion.</param>
    /// <param name="typeProvider">The type provider for content type to CLR type mapping.</param>
    /// <param name="cacheManager">Optional cache manager for caching API responses (injected when EnableCaching is true).</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public DeliveryClient(
        IDeliveryApi deliveryApi,
        ContentItemMapper contentItemMapper,
        IContentDeserializer contentDeserializer,
        ITypeProvider typeProvider,
        IDeliveryCacheManager? cacheManager = null,
        ILogger<DeliveryClient>? logger = null)
    {
        _deliveryApi = deliveryApi ?? throw new ArgumentNullException(nameof(deliveryApi));
        _contentItemMapper = contentItemMapper ?? throw new ArgumentNullException(nameof(contentItemMapper));
        _contentDeserializer = contentDeserializer ?? throw new ArgumentNullException(nameof(contentDeserializer));
        _typeProvider = typeProvider ?? throw new ArgumentNullException(nameof(typeProvider));
        _cacheManager = cacheManager;
        _logger = logger;
    }

    public IItemQuery<T> GetItem<T>(string codename)
    {
        ValidateCodename(codename, nameof(codename), "Entered item codename is not valid.");
        return new ItemQuery<T>(_deliveryApi, codename, _contentItemMapper, _contentDeserializer, _cacheManager, _logger);
    }

    public IDynamicItemQuery GetItem(string codename)
    {
        ValidateCodename(codename, nameof(codename), "Entered item codename is not valid.");
        return new DynamicItemQuery(
            _deliveryApi,
            codename,
            _contentItemMapper,
            _contentDeserializer,
            _logger);
    }

    public IItemsQuery<T> GetItems<T>() => new ItemsQuery<T>(_deliveryApi, _contentItemMapper, _contentDeserializer, _typeProvider, _cacheManager, _logger);

    public IDynamicItemsQuery GetItems()
    {
        return new DynamicItemsQuery(
            _deliveryApi,
            _contentItemMapper,
            _contentDeserializer,
            _typeProvider,
            _logger);
    }

    public IEnumerateItemsQuery<T> GetItemsFeed<T>() => new EnumerateItemsQuery<T>(_deliveryApi, _contentItemMapper, _typeProvider, _logger);

    public IDynamicEnumerateItemsQuery GetItemsFeed() => new DynamicEnumerateItemsQuery(
        _deliveryApi,
        _contentItemMapper,
        _typeProvider,
        _logger);

    public ITypeQuery GetType(string codename)
    {
        ValidateCodename(codename, nameof(codename), "Entered type codename is not valid.");
        return new TypeQuery(_deliveryApi, codename, _cacheManager, _logger);
    }

    public ITypesQuery GetTypes() => new TypesQuery(_deliveryApi, _cacheManager, _logger);

    public ITypeElementQuery GetContentElement(string contentTypeCodename, string contentElementCodename)
    {
        ValidateCodename(contentTypeCodename, nameof(contentTypeCodename), "Entered content type codename is not valid.");
        ValidateCodename(contentElementCodename, nameof(contentElementCodename), "Entered content element codename is not valid.");
        return new TypeElementQuery(_deliveryApi, contentTypeCodename, contentElementCodename);
    }

    public ITaxonomyQuery GetTaxonomy(string codename)
    {
        ValidateCodename(codename, nameof(codename), "Entered taxonomy codename is not valid.");
        return new TaxonomyQuery(_deliveryApi, codename, _cacheManager);
    }

    public ITaxonomiesQuery GetTaxonomies() => new TaxonomiesQuery(_deliveryApi, _cacheManager);

    public ILanguagesQuery GetLanguages() => new LanguagesQuery(_deliveryApi);

    public IItemUsedInQuery GetItemUsedIn(string codename)
    {
        ValidateCodename(codename, nameof(codename), "Entered item codename is not valid.");
        return new ItemUsedInQuery(_deliveryApi, codename, _logger);
    }

    public IAssetUsedInQuery GetAssetUsedIn(string codename)
    {
        ValidateCodename(codename, nameof(codename), "Entered asset codename is not valid.");
        return new AssetUsedInQuery(_deliveryApi, codename, _logger);
    }

    private static void ValidateCodename(string? codename, string parameterName, string message)
    {
        if (string.IsNullOrWhiteSpace(codename))
        {
            throw new ArgumentException(message, parameterName);
        }
    }
}
