using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Caching
{
    /// <summary>
    /// Executes requests with cache against the Kentico Kontent Delivery API.
    /// </summary>
    public class DeliveryClientCache : IDeliveryClient
    {
        private readonly IDeliveryClient _deliveryClient;
        private readonly IDeliveryCacheManager _deliveryCacheManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClientCache"/> class for retrieving cached content of the specified project.
        /// </summary>
        /// <param name="cacheManager"></param>
        /// <param name="deliveryClient"></param>
        public DeliveryClientCache(IDeliveryCacheManager cacheManager, IDeliveryClient deliveryClient)
        {
            _deliveryClient = deliveryClient ?? throw new ArgumentNullException(nameof(deliveryClient));
            _deliveryCacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        }

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of linked items.</param>
        /// <returns>The <see cref="IDeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<IDeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetItemKey<T>(codename, queryParameters),
                () => _deliveryClient.GetItemAsync<T>(codename, queryParameters),
                response => response != null,
                CacheHelpers.GetItemDependencies);
        }

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object" /> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IDeliveryItemListingResponse{T}" /> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<IDeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetItemsKey<T>(queryParameters),
                () => _deliveryClient.GetItemsAsync<T>(queryParameters),
                response => response.Items.Any(),
                CacheHelpers.GetItemsDependencies);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed content items matching the optional filtering parameters.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object" /> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{T}" /> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        public IDeliveryItemsFeed<T> GetItemsFeed<T>(IEnumerable<IQueryParameter> parameters)
        {
            return _deliveryClient.GetItemsFeed<T>(parameters);
        }

        /// <summary>
        /// Returns a content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The content type with the specified codename.</returns>
        public async Task<IDeliveryTypeResponse> GetTypeAsync(string codename)
        {
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetTypeKey(codename),
                () => _deliveryClient.GetTypeAsync(codename),
                response => response != null,
                CacheHelpers.GetTypeDependencies);
        }

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example for paging.</param>
        /// <returns>The <see cref="IDeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<IDeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter> parameters)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetTypesKey(queryParameters),
                () => _deliveryClient.GetTypesAsync(queryParameters),
                response => response.Types.Any(),
                CacheHelpers.GetTypesDependencies);
        }

        /// <summary>
        /// Returns a content element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content element.</param>
        /// <returns>A content element with the specified codename that is a part of a content type with the specified codename.</returns>
        public async Task<IDeliveryElementResponse> GetContentElementAsync(string contentTypeCodename, string contentElementCodename)
        {
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetContentElementKey(contentTypeCodename, contentElementCodename),
                () => _deliveryClient.GetContentElementAsync(contentTypeCodename, contentElementCodename),
                response => response != null,
                CacheHelpers.GetContentElementDependencies);
        }

        /// <summary>
        /// Returns a taxonomy group.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The taxonomy group with the specified codename.</returns>
        public async Task<IDeliveryTaxonomyResponse> GetTaxonomyAsync(string codename)
        {
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetTaxonomyKey(codename),
                () => _deliveryClient.GetTaxonomyAsync(codename),
                response => response != null,
                CacheHelpers.GetTaxonomyDependencies);
        }

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public async Task<IDeliveryTaxonomyListingResponse> GetTaxonomiesAsync(IEnumerable<IQueryParameter> parameters)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetTaxonomiesKey(queryParameters),
                () => _deliveryClient.GetTaxonomiesAsync(queryParameters),
                response => response.Taxonomies.Any(),
                CacheHelpers.GetTaxonomiesDependencies);
        }

        /// <summary>
        /// Returns languages.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryLanguageListingResponse"/> instance that represents the languages. If no query parameters are specified, all languages are returned.</returns>
        public async Task<IDeliveryLanguageListingResponse> GetLanguagesAsync(IEnumerable<IQueryParameter> parameters = null)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetLanguagesKey(queryParameters),
                () => _deliveryClient.GetLanguagesAsync(queryParameters),
                response => response.Languages.Any(),
                CacheHelpers.GetlanguagesDependencies);
        }
    }
}
