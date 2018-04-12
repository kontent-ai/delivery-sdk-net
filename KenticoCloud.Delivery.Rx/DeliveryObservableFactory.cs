using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery.Rx
{
    public static class DeliveryObservableFactory
    {
        #region "Public methods"

        public static IObservable<JObject> ItemJson(IDeliveryClient deliveryClient, string codename, params string[] parameters)
        {
            return GetJsonObservableOfOne(() => deliveryClient?.GetItemJsonAsync(codename, parameters)?.Result);
        }

        public static IObservable<JObject> ItemsJson(IDeliveryClient deliveryClient, params string[] parameters)
        {
            return GetJsonObservableOfOne(() => deliveryClient?.GetItemsJsonAsync(parameters)?.Result);
        }

        public static IObservable<ContentItem> Item(IDeliveryClient deliveryClient, string codename, params IQueryParameter[] parameters)
        {
            return Item(deliveryClient, codename, (IEnumerable<IQueryParameter>)parameters);
        }

        public static IObservable<T> Item<T>(IDeliveryClient deliveryClient, string codename, params IQueryParameter[] parameters)
            where T : class
        {
            return Item<T>(deliveryClient, codename, (IEnumerable<IQueryParameter>)parameters);
        }

        public static IObservable<ContentItem> Item(IDeliveryClient deliveryClient, string codename, IEnumerable<IQueryParameter> parameters)
        {
            return GetObservableOfOne(() => deliveryClient?.GetItemAsync(codename, parameters)?.Result?.Item);
        }

        public static IObservable<T> Item<T>(IDeliveryClient deliveryClient, string codename, IEnumerable<IQueryParameter> parameters = null)
            where T : class
        {
            return GetObservableOfOne(() => deliveryClient?.GetItemAsync<T>(codename, parameters)?.Result?.Item);
        }

        public static IObservable<ContentItem> Items(IDeliveryClient deliveryClient, params IQueryParameter[] parameters)
        {
            return Items(deliveryClient, (IEnumerable<IQueryParameter>)parameters);
        }

        public static IObservable<ContentItem> Items(IDeliveryClient deliveryClient, IEnumerable<IQueryParameter> parameters)
        {
            return (deliveryClient?.GetItemsAsync(parameters))?.Result?.Items?.ToObservable();
        }

        public static IObservable<T> Items<T>(IDeliveryClient deliveryClient, params IQueryParameter[] parameters)
            where T : class
        {
            return Items<T>(deliveryClient, (IEnumerable<IQueryParameter>)parameters);
        }

        public static IObservable<T> Items<T>(IDeliveryClient deliveryClient, IEnumerable<IQueryParameter> parameters)
            where T : class
        {
            return (deliveryClient?.GetItemsAsync<T>(parameters))?.Result?.Items?.ToObservable();
        }

        public static IObservable<JObject> TypeJson(IDeliveryClient deliveryClient, string codename)
        {
            return GetJsonObservableOfOne(() => deliveryClient?.GetTypeJsonAsync(codename)?.Result);
        }

        public static IObservable<JObject> TypesJson(IDeliveryClient deliveryClient, params string[] parameters)
        {
            return GetJsonObservableOfOne(() => deliveryClient?.GetTypesJsonAsync(parameters)?.Result);
        }

        public static IObservable<ContentType> Type(IDeliveryClient deliveryClient, string codename)
        {
            return GetObservableOfOne(() => deliveryClient?.GetTypeAsync(codename)?.Result);
        }

        public static IObservable<ContentType> Types(IDeliveryClient deliveryClient, params IQueryParameter[] parameters)
        {
            return Types(deliveryClient, (IEnumerable<IQueryParameter>)parameters);
        }

        public static IObservable<ContentType> Types(IDeliveryClient deliveryClient, IEnumerable<IQueryParameter> parameters)
        {
            return (deliveryClient?.GetTypesAsync(parameters))?.Result?.Types?.ToObservable();
        }

        public static IObservable<ContentElement> Element(IDeliveryClient deliveryClient, string contentTypeCodename, string contentElementCodename)
        {
            return GetObservableOfOne(() => deliveryClient?.GetContentElementAsync(contentTypeCodename, contentElementCodename)?.Result);
        }

        public static IObservable<JObject> TaxonomyJson(IDeliveryClient deliveryClient, string codename)
        {
            return GetJsonObservableOfOne(() => deliveryClient?.GetTaxonomyJsonAsync(codename)?.Result);
        }

        public static IObservable<JObject> TaxonomiesJson(IDeliveryClient deliveryClient, params string[] parameters)
        {
            return GetJsonObservableOfOne(() => deliveryClient?.GetTaxonomiesJsonAsync(parameters)?.Result);
        }

        public static IObservable<TaxonomyGroup> Taxonomy(IDeliveryClient deliveryClient, string codename)
        {
            return GetObservableOfOne(() => deliveryClient?.GetTaxonomyAsync(codename)?.Result);
        }

        public static IObservable<TaxonomyGroup> Taxonomies(IDeliveryClient deliveryClient, params IQueryParameter[] parameters)
        {
            return Taxonomies(deliveryClient, (IEnumerable<IQueryParameter>)parameters);
        }

        public static IObservable<TaxonomyGroup> Taxonomies(IDeliveryClient deliveryClient, IEnumerable<IQueryParameter> parameters)
        {
            return (deliveryClient?.GetTaxonomiesAsync(parameters))?.Result?.Taxonomies?.ToObservable();
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
