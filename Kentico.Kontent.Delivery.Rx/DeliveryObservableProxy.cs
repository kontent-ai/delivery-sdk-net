using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentTypes;
using Kentico.Kontent.Delivery.Abstractions.ContentTypes.Element;
using Kentico.Kontent.Delivery.Abstractions.TaxonomyGroups;

namespace Kentico.Kontent.Delivery.Rx
{
    /// <summary>
    /// A class that enables a reactive way of consuming data from Kentico Kontent
    /// </summary>
    public class DeliveryObservableProxy
    {
        #region "Properties"

        /// <summary>
        /// A property that gets the <see cref="IDeliveryClient" /> instance.
        /// </summary>
        public IDeliveryClient DeliveryClient { get; }

        #endregion

        #region "Constructors"

        /// <summary>
        /// Creates an object that enables reactive way of consuming data from Kentico Kontent
        /// </summary>
        /// <param name="deliveryClient">A <see cref="IDeliveryClient"/> instance.</param>
        public DeliveryObservableProxy(IDeliveryClient deliveryClient)
        {
            DeliveryClient = deliveryClient ?? throw new ArgumentNullException(nameof(deliveryClient));
        }

        #endregion

        #region "Public methods"

        /// <summary>
        /// Gets an observable of a single, strongly typed content item, by its codename. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{T}"/> that represents the content item with the specified codename.</returns>
        public IObservable<T> GetItemObservable<T>(string codename, params IQueryParameter[] parameters)
            where T : class
        {
            return GetItemObservable<T>(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Gets an observable of a single strongly typed content item, by its codename. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{T}"/> that represents the content item with the specified codename.</returns>
        public IObservable<T> GetItemObservable<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
            where T : class
        {
            return GetObservableOfOne(() => DeliveryClient?.GetItemAsync<T>(codename, parameters)?.Result?.Item);
        }

        /// <summary>
        /// Returns an observable of strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{T}"/> that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public IObservable<T> GetItemsObservable<T>(params IQueryParameter[] parameters)
            where T : class
        {
            return GetItemsObservable<T>((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns an observable of strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{T}"/> that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public IObservable<T> GetItemsObservable<T>(IEnumerable<IQueryParameter> parameters)
            where T : class
        {
            return (DeliveryClient?.GetItemsAsync<T>(parameters))?.Result?.Items?.ToObservable();
        }

        /// <summary>
        /// Returns an observable of strongly typed content items that match the optional filtering parameters. Items are enumerated in batches.
        /// </summary>
        /// /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IObservable{T}"/> that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public IObservable<T> GetItemsFeedObservable<T>(params IQueryParameter[] parameters) where T : class
        {
            return GetItemsFeedObservable<T>((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns an observable of strongly typed content items that match the optional filtering parameters. Items are enumerated in batches.
        /// </summary>
        /// /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IObservable{T}"/> that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public IObservable<T> GetItemsFeedObservable<T>(IEnumerable<IQueryParameter> parameters) where T : class
        {
            var feed = DeliveryClient?.GetItemsFeed<T>(parameters);
            return feed == null ? null : EnumerateFeed()?.ToObservable();

            IEnumerable<T> EnumerateFeed()
            {
                while (feed.HasMoreResults)
                {
                    foreach (var contentItem in feed.FetchNextBatchAsync().Result)
                    {
                        yield return contentItem;
                    }
                }
            }
        }

        /// <summary>
        /// Returns an observable of a single content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="IObservable{ContentType}"/> that represents the content type with the specified codename.</returns>
        public IObservable<IContentType> GetTypeObservable(string codename)
        {
            return GetObservableOfOne(() => DeliveryClient?.GetTypeAsync(codename)?.Result.Type);
        }

        /// <summary>
        /// Returns an observable of content types.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{ContentType}"/> that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public IObservable<IContentType> GetTypesObservable(params IQueryParameter[] parameters)
        {
            return GetTypesObservable((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns an observable of content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{ContentType}"/> that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public IObservable<IContentType> GetTypesObservable(IEnumerable<IQueryParameter> parameters)
        {
            return (DeliveryClient?.GetTypesAsync(parameters))?.Result?.Types?.ToObservable();
        }
        /// <summary>
        /// Returns an observable of a single content element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content element.</param>
        /// <returns>An <see cref="IObservable{ContentElement}"/> that represents the content element with the specified codename, that is a part of a content type with the specified codename.</returns>
        public IObservable<IContentElement> GetElementObservable(string contentTypeCodename, string contentElementCodename)
        {
            return GetObservableOfOne(() => DeliveryClient?.GetContentElementAsync(contentTypeCodename, contentElementCodename)?.Result.Element);
        }

        /// <summary>
        /// Returns an observable of a single taxonomy group.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="IObservable{TaxonomyGroup}"/> that represents the taxonomy group with the specified codename.</returns>
        public IObservable<ITaxonomyGroup> GetTaxonomyObservable(string codename)
        {
            return GetObservableOfOne(() => DeliveryClient?.GetTaxonomyAsync(codename)?.Result.Taxonomy);
        }

        /// <summary>
        /// Returns an observable of taxonomy groups.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{TaxonomyGroup}"/> that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public IObservable<ITaxonomyGroup> GetTaxonomiesObservable(params IQueryParameter[] parameters)
        {
            return GetTaxonomiesObservable((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns an observable of taxonomy groups.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{TaxonomyGroup}"/> that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public IObservable<ITaxonomyGroup> GetTaxonomiesObservable(IEnumerable<IQueryParameter> parameters)
        {
            return (DeliveryClient?.GetTaxonomiesAsync(parameters))?.Result?.Taxonomies?.ToObservable();
        }

        #endregion
        #region "Private methods"

        private static IObservable<T> GetObservableOfOne<T>(Func<T> responseFactory)
        {
            return Observable.Create((IObserver<T> observer) =>
            {
                try
                {
                    observer.OnNext(responseFactory());
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return Disposable.Empty;
            });
        }

        #endregion
    }
}
