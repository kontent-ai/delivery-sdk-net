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
        private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving content of the specified environment.
        /// </summary>
        /// <param name="deliveryApi">The Refit-generated API client.</param>
        /// <param name="deliveryOptions">The settings of the Kontent.ai environment.</param>
        public DeliveryClient(
            IDeliveryApi deliveryApi,
            IOptionsMonitor<DeliveryOptions> deliveryOptions)
        {
            _deliveryApi = deliveryApi ?? throw new ArgumentNullException(nameof(deliveryApi));
            _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));
        }

        public ISingleItemQuery<T> GetItem<T>(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            return new SingleItemQuery<T>(_deliveryApi, codename);
        }

        public IMultipleItemsQuery<T> GetItems<T>()
        {
            return new MultipleItemsQuery<T>(_deliveryApi);
        }

        public IEnumerateItemsQuery<T> GetItemsFeed<T>()
        {
            return new EnumerateItemsQuery<T>(_deliveryApi);
        }

        public ITypeQuery GetType(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered type codename is not valid.", nameof(codename));
            }

            return new TypeQuery(_deliveryApi, codename);
        }

        public ITypesQuery GetTypes()
        {
            return new TypesQuery(_deliveryApi);
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

            return new TypeElementQuery(_deliveryApi, contentTypeCodename, contentElementCodename);
        }

        public ITaxonomyQuery GetTaxonomy(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered taxonomy codename is not valid.", nameof(codename));
            }

            return new TaxonomyQuery(_deliveryApi, codename);
        }

        public ITaxonomiesQuery GetTaxonomies()
        {
            return new TaxonomiesQuery(_deliveryApi);
        }

        public ILanguagesQuery GetLanguages()
        {
            return new LanguagesQuery(_deliveryApi);
        }

        public IItemUsedInQuery GetItemUsedIn(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            return new ItemUsedInQuery(_deliveryApi, codename);
        }

        public IAssetUsedInQuery GetAssetUsedIn(string codename)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered asset codename is not valid.", nameof(codename));
            }

            return new AssetUsedInQuery(_deliveryApi, codename);
        }
    }
}