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
/// <param name="elementsPostProcessor">The elements post processor.</param>
/// <param name="cacheManager">Optional cache manager for caching API responses (injected when EnableCaching is true).</param>
internal sealed class DeliveryClient(
    IDeliveryApi deliveryApi,
    IOptionsMonitor<DeliveryOptions> deliveryOptions,
    IElementsPostProcessor elementsPostProcessor,
    IDeliveryCacheManager? cacheManager = null) : IDeliveryClient
{
    private readonly IDeliveryApi _deliveryApi = deliveryApi ?? throw new ArgumentNullException(nameof(deliveryApi));
    private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));
    private readonly IElementsPostProcessor _elementsPostProcessor = elementsPostProcessor ?? throw new ArgumentNullException(nameof(elementsPostProcessor));
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;

    public IItemQuery<T> GetItem<T>(string codename) where T : IElementsModel
    {
        if (string.IsNullOrEmpty(codename))
        {
            throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
        }

        return new ItemQuery<T>(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent, _elementsPostProcessor, _cacheManager);
    }

    public IDynamicItemQuery GetItem(string codename)
    {
        if (string.IsNullOrEmpty(codename))
        {
            throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
        }

        return new DynamicItemQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent);
    }

    public IItemsQuery<T> GetItems<T>() where T : IElementsModel
    {
        return new ItemsQuery<T>(_deliveryApi, GetDefaultWaitForLoadingNewContent, _elementsPostProcessor, _cacheManager);
    }

    public IDynamicItemsQuery GetItems()
    {
        return new DynamicItemsQuery(_deliveryApi, GetDefaultWaitForLoadingNewContent);
    }

    public IEnumerateItemsQuery<T> GetItemsFeed<T>() where T : IElementsModel
    {
        return new EnumerateItemsQuery<T>(_deliveryApi, GetDefaultWaitForLoadingNewContent, _elementsPostProcessor);
    }

    public IEnumerateItemsQuery<IElementsModel> GetItemsFeed()
    {
        return new EnumerateItemsQuery<IElementsModel>(_deliveryApi, GetDefaultWaitForLoadingNewContent, _elementsPostProcessor);
    }

    public ITypeQuery GetType(string codename)
    {
        if (string.IsNullOrEmpty(codename))
        {
            throw new ArgumentException("Entered type codename is not valid.", nameof(codename));
        }

        return new TypeQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent, _cacheManager);
    }

    public ITypesQuery GetTypes()
    {
        return new TypesQuery(_deliveryApi, GetDefaultWaitForLoadingNewContent, _cacheManager);
    }

    public ITypeElementQuery GetContentElement(string contentTypeCodename, string contentElementCodename)
    {
        if (string.IsNullOrEmpty(contentTypeCodename))
        {
            throw new ArgumentException("Entered content type codename is not valid.", nameof(contentTypeCodename));
        }

        if (string.IsNullOrEmpty(contentElementCodename))
        {
            throw new ArgumentException("Entered content element codename is not valid.", nameof(contentElementCodename));
        }

        return new TypeElementQuery(_deliveryApi, contentTypeCodename, contentElementCodename, GetDefaultWaitForLoadingNewContent);
    }

    public ITaxonomyQuery GetTaxonomy(string codename)
    {
        if (string.IsNullOrEmpty(codename))
        {
            throw new ArgumentException("Entered taxonomy codename is not valid.", nameof(codename));
        }

        return new TaxonomyQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent, _cacheManager);
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
        if (string.IsNullOrEmpty(codename))
        {
            throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
        }

        return new ItemUsedInQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent);
    }

    public IAssetUsedInQuery GetAssetUsedIn(string codename)
    {
        if (string.IsNullOrEmpty(codename))
        {
            throw new ArgumentException("Entered asset codename is not valid.", nameof(codename));
        }

        return new AssetUsedInQuery(_deliveryApi, codename, GetDefaultWaitForLoadingNewContent);
    }

    private bool? GetDefaultWaitForLoadingNewContent()
        => _deliveryOptions.CurrentValue.WaitForLoadingNewContent ? true : null;
}