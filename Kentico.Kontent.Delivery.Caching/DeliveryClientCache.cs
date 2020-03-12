using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving cached content of the specified project.
        /// </summary>
        /// <param name="cacheManager"></param>
        /// <param name="deliveryClient"></param>
        public DeliveryClientCache(IDeliveryCacheManager cacheManager, IDeliveryClient deliveryClient)
        {
            _deliveryClient = deliveryClient ?? throw new ArgumentNullException(nameof(deliveryClient));
            _deliveryCacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        }

        /// <summary>
        /// Returns a content item as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for projection or depth of linked items.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content item with the specified codename.</returns>
        public async Task<JObject> GetItemJsonAsync(string codename, params string[] parameters)
        {
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetItemJsonKey(codename, parameters),
                () => _deliveryClient.GetItemJsonAsync(codename, parameters),
                response => response != null,
                CacheHelpers.GetItemJsonDependencies);
        }

        /// <summary>
        /// Returns content items as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for filtering, ordering or depth of linked items.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<JObject> GetItemsJsonAsync(params string[] parameters)
        {
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetItemsJsonKey(parameters),
                () => _deliveryClient.GetItemsJsonAsync(parameters),
                response => response["items"].Any(),
                CacheHelpers.GetItemsJsonDependencies);
        }

        /// <summary>
        /// Returns a content item.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for projection or depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse> GetItemAsync(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for projection or depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync<T>(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns a content item.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse> GetItemAsync(string codename, IEnumerable<IQueryParameter> parameters)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetItemKey(codename, queryParameters),
                () => _deliveryClient.GetItemAsync(codename, queryParameters),
                response => response != null,
                CacheHelpers.GetItemDependencies);
        }

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetItemTypedKey(codename, queryParameters),
                () => _deliveryClient.GetItemAsync<T>(codename, queryParameters),
                response => response != null,
                CacheHelpers.GetItemDependencies);
        }

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns content items.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for filtering, ordering or depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns content items.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example for filtering, ordering or depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(IEnumerable<IQueryParameter> parameters)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetItemsKey(queryParameters),
                () => _deliveryClient.GetItemsAsync(queryParameters),
                response => response.Items.Any(),
                CacheHelpers.GetItemsDependencies);
        }

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns strongly typed content items.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for filtering, ordering or depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync<T>((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object" /> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}" /> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetItemsTypedKey(queryParameters),
                () => _deliveryClient.GetItemsAsync<T>(queryParameters),
                response => response.Items.Any(),
                CacheHelpers.GetItemsDependencies);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed" /> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        public IDeliveryItemsFeed GetItemsFeed(params IQueryParameter[] parameters)
        {
            return _deliveryClient.GetItemsFeed(parameters);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed" /> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        public IDeliveryItemsFeed GetItemsFeed(IEnumerable<IQueryParameter> parameters)
        {
            return _deliveryClient.GetItemsFeed(parameters);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed content items matching the optional filtering parameters.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object" /> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{T}" /> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        public IDeliveryItemsFeed<T> GetItemsFeed<T>(params IQueryParameter[] parameters)
        {
            return _deliveryClient.GetItemsFeed<T>(parameters);
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
        /// Returns a content type as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content type with the specified codename.</returns>
        public async Task<JObject> GetTypeJsonAsync(string codename)
        {
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetTypeJsonKey(codename),
                () => _deliveryClient.GetTypeJsonAsync(codename),
                response => response != null,
                CacheHelpers.GetTypeJsonDependencies);
        }

        /// <summary>
        /// Returns content types as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for paging.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<JObject> GetTypesJsonAsync(params string[] parameters)
        {
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetTypesJsonKey(parameters),
                () => _deliveryClient.GetTypesJsonAsync(parameters),
                response => response["types"].Any(),
                CacheHelpers.GetTypesJsonDependencies);
        }

        /// <summary>
        /// Returns a content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The content type with the specified codename.</returns>
        public async Task<DeliveryTypeResponse> GetTypeAsync(string codename)
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
        /// <param name="parameters">An array that contains zero or more query parameters, for example for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<DeliveryTypeListingResponse> GetTypesAsync(params IQueryParameter[] parameters)
        {
            return await GetTypesAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<DeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter> parameters)
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
        public async Task<DeliveryElementResponse> GetContentElementAsync(string contentTypeCodename, string contentElementCodename)
        {

            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetContentElementKey(contentTypeCodename, contentElementCodename),
                () => _deliveryClient.GetContentElementAsync(contentTypeCodename, contentElementCodename),
                response => response != null,
                CacheHelpers.GetContentElementDependencies);
        }

        /// <summary>
        /// Returns a taxonomy group as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the taxonomy group with the specified codename.</returns>
        public async Task<JObject> GetTaxonomyJsonAsync(string codename)
        {
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetTaxonomyJsonKey(codename),
                () => _deliveryClient.GetTaxonomyJsonAsync(codename),
                response => response != null,
                CacheHelpers.GetTaxonomyJsonDependencies);
        }

        /// <summary>
        /// Returns taxonomy groups as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public async Task<JObject> GetTaxonomiesJsonAsync(params string[] parameters)
        {
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetTaxonomiesJsonKey(parameters),
                () => _deliveryClient.GetTaxonomiesJsonAsync(parameters),
                response => response["taxonomies"].Any(),
                CacheHelpers.GetTaxonomiesJsonDependencies);
        }

        /// <summary>
        /// Returns a taxonomy group.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The taxonomy group with the specified codename.</returns>
        public async Task<DeliveryTaxonomyResponse> GetTaxonomyAsync(string codename)
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
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public async Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(params IQueryParameter[] parameters)
        {
            return await GetTaxonomiesAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public async Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(IEnumerable<IQueryParameter> parameters)
        {
            var queryParameters = parameters?.ToList();
            return await _deliveryCacheManager.GetOrAddAsync(
                CacheHelpers.GetTaxonomiesKey(queryParameters),
                () => _deliveryClient.GetTaxonomiesAsync(queryParameters),
                response => response.Taxonomies.Any(),
                CacheHelpers.GetTaxonomiesDependencies);
        }
    }
}
