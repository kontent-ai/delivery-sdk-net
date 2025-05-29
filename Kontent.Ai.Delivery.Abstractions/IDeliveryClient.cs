using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Defines members necessary for retrieving content and its metadata from the Kontent.ai Delivery service.
    /// </summary>
    public interface IDeliveryClient
    {
        /// <summary>
        /// Returns a strongly typed content item. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IDeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        Task<IDeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null);

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IDeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        Task<IDeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters = null);

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed content items matching the optional filtering parameters.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{T}"/> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        IDeliveryItemsFeed<T> GetItemsFeed<T>(IEnumerable<IQueryParameter> parameters = null);

        /// <summary>
        /// Returns a content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="IDeliveryTypeResponse"/> instance that contains the content type with the specified codename.</returns>
        Task<IDeliveryTypeResponse> GetTypeAsync(string codename);

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        Task<IDeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter> parameters = null);

        /// <summary>
        /// Returns a content type element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content type element.</param>
        /// <returns>The <see cref="IDeliveryElementResponse"/> instance that contains the specified content type element.</returns>
        Task<IDeliveryElementResponse> GetContentElementAsync(string contentTypeCodename, string contentElementCodename);

        /// <summary>
        /// Returns a taxonomy group.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="IDeliveryTaxonomyResponse"/> instance that contains the taxonomy group with the specified codename.</returns>
        Task<IDeliveryTaxonomyResponse> GetTaxonomyAsync(string codename);

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        Task<IDeliveryTaxonomyListingResponse> GetTaxonomiesAsync(IEnumerable<IQueryParameter> parameters = null);


        /// <summary>
        /// Returns all active languages assigned to a given environment and matching the optional filtering parameters.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryLanguageListingResponse"/> instance that represents the languages. If no query parameters are specified, all languages are returned.</returns>
        Task<IDeliveryLanguageListingResponse> GetLanguagesAsync(IEnumerable<IQueryParameter> parameters = null);

        /// <summary>
        /// Initializes synchronization of changes in content items based on the specified parameters. After the initialization, you'll get an X-Continuation token in the response.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for filtering.</param>
        /// <returns>The <see cref="IDeliverySyncInitResponse"/> instance that represents the sync init response that contains continuation token needed for further sync execution.</returns>
        Task<IDeliverySyncInitResponse> PostSyncInitAsync(IEnumerable<IQueryParameter> parameters = null);

        /// <summary>
        /// Retrieve a list of delta updates to recently changed content items in the specified environment. The types of items you get is determined by the X-Continuation token you use.
        /// </summary>
        /// <returns>The <see cref="IDeliverySyncResponse"/> instance that represents the sync response that contains collection of delta updates and continuation token needed for further sync execution.</returns>
        Task<IDeliverySyncResponse> GetSyncAsync(string continuationToken);

        /// <summary>
        /// Initializes synchronization of changes in content items, content types, taxonomies and languages. After the initialization, you'll get an X-Continuation token in the response.
        /// </summary>
        /// <returns>The <see cref="IDeliverySyncV2Response"/> instance that represents the sync init response that contains continuation token needed for further sync execution.</returns>
        Task<IDeliverySyncV2Response> PostSyncV2InitAsync();

        /// <summary>
        /// Retrieve a list of delta updates to recently changed content items, content types, taxonomies or languages in the specified environment. The types of items you get is determined by the X-Continuation token you use.
        /// </summary>
        /// <returns>The <see cref="IDeliverySyncV2Response"/> instance that represents the sync response that contains collection of delta updates and continuation token needed for further sync execution.</returns>
        Task<IDeliverySyncV2Response> GetSyncV2Async(string continuationToken);

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed parent content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters for filtering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{IUsedInItem}"/> instance that can be used to enumerate through content item parents for the specified item codename. If no query parameters are specified, default language parents are enumerated.</returns>
        public IDeliveryItemsFeed<IUsedInItem> GetItemUsedIn(string codename, IEnumerable<IQueryParameter> parameters = null);

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed parent content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="codename">The codename of an asset.</param>
        /// <param name="parameters">A collection of query parameters for filtering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{IUsedInItem}"/> instance that can be used to enumerate through asset parents for the specified asset codename. If no query parameters are specified, default language parents are enumerated.</returns>
        public IDeliveryItemsFeed<IUsedInItem> GetAssetUsedIn(string codename, IEnumerable<IQueryParameter> parameters = null);
    }
}