using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.Configuration;
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
/// <param name="contentItemMapper">The content item mapper for element hydration.</param>
/// <param name="contentDeserializer">The content deserializer for JSON to object conversion.</param>
/// <param name="typeProvider">The type provider for content type to CLR type mapping.</param>
/// <param name="cacheManager">Optional cache manager for caching API responses (injected when EnableCaching is true).</param>
/// <param name="logger">Optional logger for diagnostic output.</param>
/// <param name="optionsMonitor">Options monitor used to determine runtime preview/production mode.</param>
/// <param name="clientName">Client name used for resolving named options from <paramref name="optionsMonitor"/>.</param>
internal sealed class DeliveryClient(
    IDeliveryApi deliveryApi,
    ContentItemMapper contentItemMapper,
    IContentDeserializer contentDeserializer,
    ITypeProvider typeProvider,
    IDeliveryCacheManager? cacheManager = null,
    ILogger<DeliveryClient>? logger = null,
    IOptionsMonitor<DeliveryOptions>? optionsMonitor = null,
    string? clientName = null) : IDeliveryClient
{
    private readonly string _clientName = string.IsNullOrWhiteSpace(clientName) ? DeliveryClientNames.Default : clientName;

    public IItemQuery<T> GetItem<T>(string codename)
    {
        EnsureCodenameValid(codename);
        return new ItemQuery<T>(
            deliveryApi,
            codename,
            contentItemMapper,
            contentDeserializer,
            GetEffectiveCacheManager(),
            GetDefaultRenditionPreset(),
            GetCustomAssetDomain(),
            logger);
    }

    public IDynamicItemQuery GetItem(string codename)
    {
        EnsureCodenameValid(codename);
        return new DynamicItemQuery(
            deliveryApi,
            codename,
            contentItemMapper,
            contentDeserializer,
            GetDefaultRenditionPreset(),
            GetCustomAssetDomain(),
            logger);
    }

    public IItemsQuery<T> GetItems<T>() => new ItemsQuery<T>(
        deliveryApi,
        contentItemMapper,
        contentDeserializer,
        typeProvider,
        GetEffectiveCacheManager(),
        GetDefaultRenditionPreset(),
        GetCustomAssetDomain(),
        logger);

    public IDynamicItemsQuery GetItems()
    {
        return new DynamicItemsQuery(
            deliveryApi,
            contentItemMapper,
            contentDeserializer,
            typeProvider,
            GetDefaultRenditionPreset(),
            GetCustomAssetDomain(),
            logger);
    }

    public IEnumerateItemsQuery<T> GetItemsFeed<T>() => new EnumerateItemsQuery<T>(
        deliveryApi,
        contentItemMapper,
        typeProvider,
        GetDefaultRenditionPreset(),
        GetCustomAssetDomain(),
        logger);

    public IDynamicEnumerateItemsQuery GetItemsFeed() => new DynamicEnumerateItemsQuery(
        deliveryApi,
        contentItemMapper,
        typeProvider,
        GetDefaultRenditionPreset(),
        GetCustomAssetDomain(),
        logger);

    public ITypeQuery GetType(string codename)
    {
        EnsureCodenameValid(codename);
        return new TypeQuery(deliveryApi, codename, GetEffectiveCacheManager(), logger);
    }

    public ITypesQuery GetTypes() => new TypesQuery(deliveryApi, GetEffectiveCacheManager(), logger);

    public ITypeElementQuery GetContentElement(string contentTypeCodename, string contentElementCodename)
    {
        EnsureCodenameValid(contentTypeCodename);
        EnsureCodenameValid(contentElementCodename);
        return new TypeElementQuery(deliveryApi, contentTypeCodename, contentElementCodename, logger);
    }

    public ITaxonomyQuery GetTaxonomy(string codename)
    {
        EnsureCodenameValid(codename);
        return new TaxonomyQuery(deliveryApi, codename, GetEffectiveCacheManager(), logger);
    }

    public ITaxonomiesQuery GetTaxonomies() => new TaxonomiesQuery(deliveryApi, GetEffectiveCacheManager(), logger);

    public ILanguagesQuery GetLanguages() => new LanguagesQuery(deliveryApi, logger);

    public IItemUsedInQuery GetItemUsedIn(string codename)
    {
        EnsureCodenameValid(codename);
        return new ItemUsedInQuery(deliveryApi, codename, logger);
    }

    public IAssetUsedInQuery GetAssetUsedIn(string codename)
    {
        EnsureCodenameValid(codename);
        return new AssetUsedInQuery(deliveryApi, codename, logger);
    }

    /// <summary>
    /// No-op when the client is managed by a DI container.
    /// When created via <see cref="DeliveryClientBuilder"/>, disposal is handled by <see cref="OwnedDeliveryClient"/>.
    /// </summary>
    public void Dispose() { }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => default;

    private static void EnsureCodenameValid(string? codename, [CallerArgumentExpression(nameof(codename))] string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(codename))
        {
            throw new ArgumentException($"Entered {parameterName} is not valid.", parameterName);
        }
    }

    private IDeliveryCacheManager? GetEffectiveCacheManager()
        => IsPreviewApiEnabled() ? null : cacheManager;

    private string? GetDefaultRenditionPreset()
        => optionsMonitor?.Get(_clientName).DefaultRenditionPreset;

    private Uri? GetCustomAssetDomain()
    {
        var domain = optionsMonitor?.Get(_clientName).CustomAssetDomain;
        return string.IsNullOrWhiteSpace(domain) ? null : new Uri(domain, UriKind.Absolute);
    }

    private bool IsPreviewApiEnabled()
        => optionsMonitor?.Get(_clientName).UsePreviewApi ?? false;
}
