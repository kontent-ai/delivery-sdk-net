using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery.Rx
{
    /// <summary>
    /// A class that enables a reactive way of consuming data from Kentico Cloud
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
        /// Creates an object that enables reactive way of consuming data from Kentico Cloud
        /// </summary>
        /// <param name="deliveryClient">A <see cref="IDeliveryClient"/> instance.</param>
        public DeliveryObservableProxy(IDeliveryClient deliveryClient)
        {
            DeliveryClient = deliveryClient ?? throw new ArgumentNullException(nameof(deliveryClient));
        }

        #endregion

        #region "Public methods"

        /// <summary>
        /// Returns an observable of a single content item as JSON data. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{JObject}"/> that represents the content item with the specified codename.</returns>
        public IObservable<JObject> GetItemJsonObservable(string codename, params string[] parameters)
        {
            return GetJsonObservableOfOne(() => DeliveryClient?.GetItemJsonAsync(codename, parameters)?.Result);
        }

        /// <summary>
        /// Returns an observable of content items as JSON data. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{JObject}"/> that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public IObservable<JObject> GetItemsJsonObservable(params string[] parameters)
        {
            return GetJsonObservableOfOne(() => DeliveryClient?.GetItemsJsonAsync(parameters)?.Result);
        }

        /// <summary>
        /// Returns an observable of a single content item. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{ContentItem}"/> that represents the content item with the specified codename.</returns>
        public IObservable<ContentItem> GetItemObservable(string codename, params IQueryParameter[] parameters)
        {
            return GetItemObservable(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Gets an observable of a single, strongly typed content item, by its codename. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{T}"/> that represents the content item with the specified codename.</returns>
        public IObservable<T> GetItemObservable<T>(string codename, params IQueryParameter[] parameters)
            where T : class
        {
            return GetItemObservable<T>(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns an observable of a single content item. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{ContentItem}"/> that represents the content item with the specified codename.</returns>
        public IObservable<ContentItem> GetItemObservable(string codename, IEnumerable<IQueryParameter> parameters)
        {
            return GetObservableOfOne(() => DeliveryClient?.GetItemAsync(codename, parameters)?.Result?.Item);
        }

        /// <summary>
        /// Gets an observable of a single strongly typed content item, by its codename. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{T}"/> that represents the content item with the specified codename.</returns>
        public IObservable<T> GetItemObservable<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
            where T : class
        {
            return GetObservableOfOne(() => DeliveryClient?.GetItemAsync<T>(codename, parameters)?.Result?.Item);
        }

        /// <summary>
        /// Returns an observable of content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{ContentItem}"/> that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public IObservable<ContentItem> GetItemsObservable(params IQueryParameter[] parameters)
        {
            return GetItemsObservable((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns an observable of content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{ContentItem}"/> that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public IObservable<ContentItem> GetItemsObservable(IEnumerable<IQueryParameter> parameters)
        {
            return (DeliveryClient?.GetItemsAsync(parameters))?.Result?.Items?.ToObservable();
        }

        /// <summary>
        /// Returns an observable of strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
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
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IObservable{T}"/> that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public IObservable<T> GetItemsObservable<T>(IEnumerable<IQueryParameter> parameters)
            where T : class
        {
            return (DeliveryClient?.GetItemsAsync<T>(parameters))?.Result?.Items?.ToObservable();
        }

        /// <summary>
        /// Returns an observable of a single content type as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="IObservable{JObject}"/> that represents the content type with the specified codename.</returns>
        public IObservable<JObject> GetTypeJsonObservable(string codename)
        {
            return GetJsonObservableOfOne(() => DeliveryClient?.GetTypeJsonAsync(codename)?.Result);
        }

        /// <summary>
        /// Returns an observable of content types as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{JObject}"/> that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public IObservable<JObject> GetTypesJsonObservable(params string[] parameters)
        {
            return GetJsonObservableOfOne(() => DeliveryClient?.GetTypesJsonAsync(parameters)?.Result);
        }

        /// <summary>
        /// Returns an observable of a single content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="IObservable{ContentType}"/> that represents the content type with the specified codename.</returns>
        public IObservable<ContentType> GetTypeObservable(string codename)
        {
            return GetObservableOfOne(() => DeliveryClient?.GetTypeAsync(codename)?.Result);
        }

        /// <summary>
        /// Returns an observable of content types.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{ContentType}"/> that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public IObservable<ContentType> GetTypesObservable(params IQueryParameter[] parameters)
        {
            return GetTypesObservable((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns an observable of content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{ContentType}"/> that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public IObservable<ContentType> GetTypesObservable(IEnumerable<IQueryParameter> parameters)
        {
            return (DeliveryClient?.GetTypesAsync(parameters))?.Result?.Types?.ToObservable();
        }
        /// <summary>
        /// Returns an observable of a single content element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content element.</param>
        /// <returns>An <see cref="IObservable{ContentElement}"/> that represents the content element with the specified codename, that is a part of a content type with the specified codename.</returns>
        public IObservable<ContentElement> GetElementObservable(string contentTypeCodename, string contentElementCodename)
        {
            return GetObservableOfOne(() => DeliveryClient?.GetContentElementAsync(contentTypeCodename, contentElementCodename)?.Result);
        }

        /// <summary>
        /// Returns an observable of a single taxonomy group as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="IObservable{JObject}"/> that represents the taxonomy group with the specified codename.</returns>
        public IObservable<JObject> GetTaxonomyJsonObservable(string codename)
        {
            return GetJsonObservableOfOne(() => DeliveryClient?.GetTaxonomyJsonAsync(codename)?.Result);
        }

        /// <summary>
        /// Returns an observable of taxonomy groups as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{JObject}"/> that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public IObservable<JObject> GetTaxonomiesJsonObservable(params string[] parameters)
        {
            return GetJsonObservableOfOne(() => DeliveryClient?.GetTaxonomiesJsonAsync(parameters)?.Result);
        }

        /// <summary>
        /// Returns an observable of a single taxonomy group.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="IObservable{TaxonomyGroup}"/> that represents the taxonomy group with the specified codename.</returns>
        public IObservable<TaxonomyGroup> GetTaxonomyObservable(string codename)
        {
            return GetObservableOfOne(() => DeliveryClient?.GetTaxonomyAsync(codename)?.Result);
        }

        /// <summary>
        /// Returns an observable of taxonomy groups.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{TaxonomyGroup}"/> that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public IObservable<TaxonomyGroup> GetTaxonomiesObservable(params IQueryParameter[] parameters)
        {
            return GetTaxonomiesObservable((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns an observable of taxonomy groups.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IObservable{TaxonomyGroup}"/> that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public IObservable<TaxonomyGroup> GetTaxonomiesObservable(IEnumerable<IQueryParameter> parameters)
        {
            return (DeliveryClient?.GetTaxonomiesAsync(parameters))?.Result?.Taxonomies?.ToObservable();
        }

        #endregion

        #region "Private methods"

        private static IObservable<JObject> GetJsonObservableOfOne(Func<JObject> responseFactory)
        {
            return Observable.Create((IObserver<JObject> observer) =>
            {
                var response = responseFactory();

                if (response["error_code"] != null)
                {
                    observer.OnError(new Exception(response["message"]?.ToString()));
                }
                else
                {
                    observer.OnNext(response);
                    observer.OnCompleted();
                }

                return Disposable.Empty;
            });
        }

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
