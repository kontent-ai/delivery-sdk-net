using Microsoft.Extensions.DependencyInjection;

namespace Kontent.Ai.Delivery.Configuration;

/// <summary>
/// A delegating wrapper that owns the <see cref="ServiceProvider"/> lifetime.
/// Created by <see cref="DeliveryClientBuilder"/> so that disposing the client
/// tears down the internal service provider and all registered services.
/// </summary>
internal sealed class OwnedDeliveryClient(ServiceProvider serviceProvider, IDeliveryClient inner) : IDeliveryClient
{
    private int _disposeState;

    public IItemQuery<T> GetItem<T>(string codename) => inner.GetItem<T>(codename);
    public IDynamicItemQuery GetItem(string codename) => inner.GetItem(codename);
    public IItemsQuery<T> GetItems<T>() => inner.GetItems<T>();
    public IDynamicItemsQuery GetItems() => inner.GetItems();
    public IEnumerateItemsQuery<T> GetItemsFeed<T>() => inner.GetItemsFeed<T>();
    public IDynamicEnumerateItemsQuery GetItemsFeed() => inner.GetItemsFeed();
    public ITypeQuery GetType(string codename) => inner.GetType(codename);
    public ITypesQuery GetTypes() => inner.GetTypes();
    public ITypeElementQuery GetContentElement(string contentTypeCodename, string contentElementCodename)
        => inner.GetContentElement(contentTypeCodename, contentElementCodename);
    public ITaxonomyQuery GetTaxonomy(string codename) => inner.GetTaxonomy(codename);
    public ITaxonomiesQuery GetTaxonomies() => inner.GetTaxonomies();
    public ILanguagesQuery GetLanguages() => inner.GetLanguages();
    public IItemUsedInQuery GetItemUsedIn(string codename) => inner.GetItemUsedIn(codename);
    public IAssetUsedInQuery GetAssetUsedIn(string codename) => inner.GetAssetUsedIn(codename);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        serviceProvider.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        await serviceProvider.DisposeAsync().ConfigureAwait(false);
    }
}
