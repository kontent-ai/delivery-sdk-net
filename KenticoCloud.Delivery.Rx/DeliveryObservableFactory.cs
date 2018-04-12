using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using KenticoCloud.Delivery.InlineContentItems;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery.Rx
{
    public class DeliveryObservableFactory
    {
        #region "Properties"

        public DeliveryClient DeliveryClient { get; set; }

        #endregion

        #region "Constructors"

        public DeliveryObservableFactory(DeliveryOptions deliveryOptions)
        {
            DeliveryClient = new DeliveryClient(deliveryOptions);
        }

        public DeliveryObservableFactory(IOptions<DeliveryOptions> deliveryOptions, IContentLinkUrlResolver contentLinkUrlResolver, IInlineContentItemsProcessor contentItemsProcessor, ICodeFirstModelProvider codeFirstModelProvider)
        {
            DeliveryClient = new DeliveryClient(deliveryOptions, contentLinkUrlResolver, contentItemsProcessor, codeFirstModelProvider);
        }

        public DeliveryObservableFactory(string projectId)
        {
            DeliveryClient = !string.IsNullOrEmpty(projectId) ? new DeliveryClient(projectId) : throw new ArgumentNullException(nameof(projectId));
        }

        public DeliveryObservableFactory(string projectId, string previewApiKey)
        {
            DeliveryClient = new DeliveryClient(projectId, previewApiKey);
        }

        #endregion

        #region "Public methods"

        public IObservable<JObject> ItemJson(string codename, params string[] parameters)
        {
            return GetJsonObservableOfOne(() => DeliveryClient.GetItemJsonAsync(codename, parameters).Result);
        }

        public IObservable<JObject> ItemsJson(params string[] parameters)
        {
            return GetJsonObservableOfOne(() => DeliveryClient.GetItemsJsonAsync(parameters).Result);
        }

        public IObservable<ContentItem> Item(string codename, params IQueryParameter[] parameters)
        {
            return Item(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        public IObservable<T> Item<T>(string codename, params IQueryParameter[] parameters)
        {
            return Item<T>(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        public IObservable<ContentItem> Item(string codename, IEnumerable<IQueryParameter> parameters)
        {
            return GetObservableOfOne(() => DeliveryClient.GetItemAsync(codename, parameters).Result.Item);
        }

        public IObservable<T> Item<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            return GetObservableOfOne(() => DeliveryClient.GetItemAsync<T>(codename, parameters).Result.Item);
        }

        public IObservable<ContentItem> Items(params IQueryParameter[] parameters)
        {
            return Items((IEnumerable<IQueryParameter>)parameters);
        }

        public IObservable<ContentItem> Items(IEnumerable<IQueryParameter> parameters)
        {
            return (DeliveryClient.GetItemsAsync(parameters)).Result.Items.ToObservable();
        }

        public IObservable<T> Items<T>(params IQueryParameter[] parameters)
        {
            return Items<T>((IEnumerable<IQueryParameter>)parameters);
        }

        public IObservable<T> Items<T>(IEnumerable<IQueryParameter> parameters)
        {
            return (DeliveryClient.GetItemsAsync<T>(parameters)).Result.Items.ToObservable();
        }

        public IObservable<JObject> TypeJson(string codename)
        {
            return GetJsonObservableOfOne(() => DeliveryClient.GetTypeJsonAsync(codename).Result);
        }

        public IObservable<JObject> TypesJson(params string[] parameters)
        {
            return GetJsonObservableOfOne(() => DeliveryClient.GetTypesJsonAsync(parameters).Result);
        }

        public IObservable<ContentType> Type(string codename)
        {
            return GetObservableOfOne(() => DeliveryClient.GetTypeAsync(codename).Result);
        }

        public IObservable<ContentType> Types(params IQueryParameter[] parameters)
        {
            return Types((IEnumerable<IQueryParameter>)parameters);
        }

        public IObservable<ContentType> Types(IEnumerable<IQueryParameter> parameters)
        {
            return (DeliveryClient.GetTypesAsync(parameters)).Result.Types.ToObservable();
        }

        public IObservable<ContentElement> Element(string contentTypeCodename, string contentElementCodename)
        {
            return GetObservableOfOne(() => DeliveryClient.GetContentElementAsync(contentTypeCodename, contentElementCodename).Result);
        }

        public IObservable<JObject> TaxonomyJson(string codename)
        {
            return GetJsonObservableOfOne(() => DeliveryClient.GetTaxonomyJsonAsync(codename).Result);
        }

        public IObservable<JObject> TaxonomiesJson(params string[] parameters)
        {
            return GetJsonObservableOfOne(() => DeliveryClient.GetTaxonomiesJsonAsync(parameters).Result);
        }

        public IObservable<TaxonomyGroup> Taxonomy(string codename)
        {
            return GetObservableOfOne(() => DeliveryClient.GetTaxonomyAsync(codename).Result);
        }

        public IObservable<TaxonomyGroup> Taxonomies(params IQueryParameter[] parameters)
        {
            return Taxonomies((IEnumerable<IQueryParameter>)parameters);
        }

        public IObservable<TaxonomyGroup> Taxonomies(IEnumerable<IQueryParameter> parameters)
        {
            return (DeliveryClient.GetTaxonomiesAsync(parameters)).Result.Taxonomies.ToObservable();
        }

        #endregion

        #region "Protected methods"

        protected IObservable<JObject> GetJsonObservableOfOne(Func<JObject> responseFactory)
        {
            return Observable.Create((IObserver<JObject> observer) =>
            {
                var response = responseFactory();

                if (response["error_code"] != null)
                {
                    observer.OnError(new Exception(response["message"].ToString()));
                }
                else
                {
                    observer.OnNext(response);
                    observer.OnCompleted();
                }

                return Disposable.Empty;
            });
        }

        protected IObservable<T> GetObservableOfOne<T>(Func<T> valueFactory)
        {
            return Observable.Create((IObserver<T> observer) =>
            {
                try
                {
                    observer.OnNext(valueFactory());
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
