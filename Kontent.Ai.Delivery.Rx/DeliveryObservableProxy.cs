using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Rx;

/// <summary>
/// A class that enables a reactive way of consuming data from Kontent.ai using the new query builder pattern.
/// </summary>
/// <remarks>
/// Creates an object that enables reactive way of consuming data from Kontent.ai
/// </remarks>
/// <param name="deliveryClient">A <see cref="IDeliveryClient"/> instance.</param>
public class DeliveryObservableProxy(IDeliveryClient deliveryClient)
{
    /// <summary>
    /// A property that gets the <see cref="IDeliveryClient" /> instance.
    /// </summary>
    public IDeliveryClient DeliveryClient { get; } = deliveryClient ?? throw new ArgumentNullException(nameof(deliveryClient));

    /// <summary>
    /// Gets an observable of a single strongly typed content item, by its codename.
    /// </summary>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <param name="codename">The codename of a content item.</param>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{T}"/> that represents the content item with the specified codename.</returns>
    public IObservable<T> GetItemObservable<T>(string codename, Action<IItemQuery<T>>? configureQuery = null)
        where T : class, IElementsModel
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetItem<T>(codename);
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservable());
    }

    /// <summary>
    /// Gets an observable of a single content item with dynamic mapping, by its codename.
    /// </summary>
    /// <param name="codename">The codename of a content item.</param>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{IContentItem}"/> that represents the content item.</returns>
    public IObservable<IContentItem<IElementsModel>> GetDynamicItemObservable(string codename, Action<IDynamicItemQuery>? configureQuery = null)
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetItem(codename);
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservableDynamic());
    }

    /// <summary>
    /// Returns an observable of strongly typed content items.
    /// </summary>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{T}"/> that represents the content items.</returns>
    public IObservable<T> GetItemsObservable<T>(Action<IItemsQuery<T>>? configureQuery = null)
        where T : class, IElementsModel
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetItems<T>();
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservableMany());
    }

    /// <summary>
    /// Returns an observable of content items with dynamic mapping.
    /// </summary>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{IContentItem}"/> that represents the content items.</returns>
    public IObservable<IContentItem<IElementsModel>> GetDynamicItemsObservable(Action<IDynamicItemsQuery>? configureQuery = null)
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetItems();
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservableDynamicMany());
    }

    /// <summary>
    /// Returns an observable of strongly typed content items using feed enumeration.
    /// </summary>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{T}"/> that represents the content items enumerated from the feed.</returns>
    public IObservable<T> GetItemsFeedObservable<T>(Action<IEnumerateItemsQuery<T>>? configureQuery = null) 
        where T : class, IElementsModel
    {
        return Observable.Create<T>(async (observer, ct) =>
        {
            try
            {
                var query = DeliveryClient.GetItemsFeed<T>();
                configureQuery?.Invoke(query);
                
                await foreach (var item in query.EnumerateItemsAsync().WithCancellation(ct).ConfigureAwait(false))
                {
                    observer.OnNext(item.Elements);
                }
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });
    }

    /// <summary>
    /// Returns an observable of content items with dynamic mapping using feed enumeration.
    /// </summary>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{IContentItem}"/> that represents the content items enumerated from the feed.</returns>
    public IObservable<IContentItem<IElementsModel>> GetDynamicItemsFeedObservable(Action<IEnumerateItemsQuery<IElementsModel>>? configureQuery = null)
    {
        return Observable.Create<IContentItem<IElementsModel>>(async (observer, ct) =>
        {
            try
            {
                var query = DeliveryClient.GetItemsFeed();
                configureQuery?.Invoke(query);
                
                await foreach (var item in query.EnumerateItemsAsync().WithCancellation(ct).ConfigureAwait(false))
                {
                    observer.OnNext(item);
                }
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });
    }

    /// <summary>
    /// Returns an observable of parent content items for specified content item.
    /// </summary>
    /// <param name="codename">The codename of a content item.</param>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{IUsedInItem}"/> that represents the parent content items.</returns>
    public IObservable<IUsedInItem> GetItemUsedInObservable(string codename, Action<IItemUsedInQuery>? configureQuery = null)
    {
        return Observable.Create<IUsedInItem>(async (observer, ct) =>
        {
            try
            {
                var query = DeliveryClient.GetItemUsedIn(codename);
                configureQuery?.Invoke(query);
                
                await foreach (var item in query.EnumerateItemsAsync().WithCancellation(ct).ConfigureAwait(false))
                {
                    observer.OnNext(item);
                }
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });
    }

    /// <summary>
    /// Returns an observable of parent content items for specified asset.
    /// </summary>
    /// <param name="codename">The codename of an asset.</param>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{IUsedInItem}"/> that represents the parent content items.</returns>
    public IObservable<IUsedInItem> GetAssetUsedInObservable(string codename, Action<IAssetUsedInQuery>? configureQuery = null)
    {
        return Observable.Create<IUsedInItem>(async (observer, ct) =>
        {
            try
            {
                var query = DeliveryClient.GetAssetUsedIn(codename);
                configureQuery?.Invoke(query);
                
                await foreach (var item in query.EnumerateItemsAsync().WithCancellation(ct).ConfigureAwait(false))
                {
                    observer.OnNext(item);
                }
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });
    }

    /// <summary>
    /// Returns an observable of a single content type.
    /// </summary>
    /// <param name="codename">The codename of a content type.</param>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{IContentType}"/> that represents the content type with the specified codename.</returns>
    public IObservable<IContentType> GetTypeObservable(string codename, Action<ITypeQuery>? configureQuery = null)
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetType(codename);
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservableType());
    }

    /// <summary>
    /// Returns an observable of content types.
    /// </summary>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{IContentType}"/> that represents the content types.</returns>
    public IObservable<IContentType> GetTypesObservable(Action<ITypesQuery>? configureQuery = null)
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetTypes();
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservableTypes());
    }
    /// <summary>
    /// Returns an observable of a single content element.
    /// </summary>
    /// <param name="contentTypeCodename">The codename of the content type.</param>
    /// <param name="contentElementCodename">The codename of the content element.</param>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>An <see cref="IObservable{IContentElement}"/> that represents the content element.</returns>
    public IObservable<IContentElement> GetElementObservable(string contentTypeCodename, string contentElementCodename, Action<ITypeElementQuery>? configureQuery = null)
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetContentElement(contentTypeCodename, contentElementCodename);
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservableElement());
    }

    /// <summary>
    /// Returns an observable of a single taxonomy group.
    /// </summary>
    /// <param name="codename">The codename of a taxonomy group.</param>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{ITaxonomyGroup}"/> that represents the taxonomy group with the specified codename.</returns>
    public IObservable<ITaxonomyGroup> GetTaxonomyObservable(string codename, Action<ITaxonomyQuery>? configureQuery = null)
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetTaxonomy(codename);
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservableTaxonomy());
    }

    /// <summary>
    /// Returns an observable of taxonomy groups.
    /// </summary>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{ITaxonomyGroup}"/> that represents the taxonomy groups.</returns>
    public IObservable<ITaxonomyGroup> GetTaxonomiesObservable(Action<ITaxonomiesQuery>? configureQuery = null)
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetTaxonomies();
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservableTaxonomies());
    }

    /// <summary>
    /// Returns an observable of languages.
    /// </summary>
    /// <param name="configureQuery">Optional action to configure the query.</param>
    /// <returns>The <see cref="IObservable{ILanguage}"/> that represents the languages.</returns>
    public IObservable<ILanguage> GetLanguagesObservable(Action<ILanguagesQuery>? configureQuery = null)
    {
        return Observable.FromAsync(async (CancellationToken ct) =>
        {
            var query = DeliveryClient.GetLanguages();
            configureQuery?.Invoke(query);
            return await query.ExecuteAsync(ct).ConfigureAwait(false);
        })
        .SelectMany(result => result.ToObservableLanguages());
    }
}
