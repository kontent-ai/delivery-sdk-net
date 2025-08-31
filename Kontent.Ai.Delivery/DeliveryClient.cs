using Microsoft.Extensions.Options;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery
{
    /// <summary>
    /// Executes requests against the Kontent.ai Delivery API using query builders.
    /// </summary>
    internal sealed class DeliveryClient : IDeliveryClient
    {
        private readonly IDeliveryApi _deliveryApi;
        private readonly DeliveryResponseProcessor _responseProcessor;
        private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving content of the specified environment.
        /// </summary>
        /// <param name="deliveryApi">The Refit-generated API client.</param>
        /// <param name="responseProcessor">The response processor for handling API responses.</param>
        /// <param name="deliveryOptions">The settings of the Kontent.ai environment.</param>
        public DeliveryClient(
            IDeliveryApi deliveryApi,
            DeliveryResponseProcessor responseProcessor,
            IOptionsMonitor<DeliveryOptions> deliveryOptions)
        {
            _deliveryApi = deliveryApi ?? throw new ArgumentNullException(nameof(deliveryApi));
            _responseProcessor = responseProcessor ?? throw new ArgumentNullException(nameof(responseProcessor));
            _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));
        }

        public ISingleItemQuery<T> GetItem<T>(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            return new SingleItemQuery<T>(_deliveryApi, codename, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public ISingleItemQuery<object> GetItem(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            return new SingleItemQuery<object>(_deliveryApi, codename, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public IMultipleItemsQuery<T> GetItems<T>()
        {
            return new MultipleItemsQuery<T>(_deliveryApi, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public IMultipleItemsQuery<object> GetItems()
        {
            return new MultipleItemsQuery<object>(_deliveryApi, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public IEnumerateItemsQuery<T> GetItemsFeed<T>()
        {
            return new EnumerateItemsQuery<T>(_deliveryApi, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public IEnumerateItemsQuery<object> GetItemsFeed()
        {
            return new EnumerateItemsQuery<object>(_deliveryApi, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public ITypeQuery GetType(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered type codename is not valid.", nameof(codename));
            }

            return new TypeQuery(_deliveryApi, codename, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public ITypesQuery GetTypes()
        {
            return new TypesQuery(_deliveryApi, _responseProcessor, GetDefaultWaitForLoadingNewContent);
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

            return new TypeElementQuery(_deliveryApi, contentTypeCodename, contentElementCodename, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public ITaxonomyQuery GetTaxonomy(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered taxonomy codename is not valid.", nameof(codename));
            }

            return new TaxonomyQuery(_deliveryApi, codename, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public ITaxonomiesQuery GetTaxonomies()
        {
            return new TaxonomiesQuery(_deliveryApi, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public ILanguagesQuery GetLanguages()
        {
            return new LanguagesQuery(_deliveryApi, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public IItemUsedInQuery GetItemUsedIn(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            return new ItemUsedInQuery(_deliveryApi, codename, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        public IAssetUsedInQuery GetAssetUsedIn(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered asset codename is not valid.", nameof(codename));
            }

            return new AssetUsedInQuery(_deliveryApi, codename, _responseProcessor, GetDefaultWaitForLoadingNewContent);
        }

        private bool? GetDefaultWaitForLoadingNewContent()
            => _deliveryOptions.CurrentValue.WaitForLoadingNewContent ? true : null;
    }
}