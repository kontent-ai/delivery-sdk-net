using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Defines members necessary for retrieving content and its metadata from the Kentico Kontent Delivery service.
    /// </summary>
    public interface IDeliveryClient
    {
        /// <summary>
        /// Returns a content item as JSON data. By default, retrieves one level of modular content.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content item with the specified codename.</returns>
        Task<JObject> GetItemJsonAsync(string codename, params string[] parameters);

        /// <summary>
        /// Returns content items as JSON data. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        Task<JObject> GetItemsJsonAsync(params string[] parameters);

        /// <summary>
        /// Returns a content item. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        Task<DeliveryItemResponse> GetItemAsync(string codename, params IQueryParameter[] parameters);

        /// <summary>
        /// Returns a strongly typed content item. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, params IQueryParameter[] parameters);

        /// <summary>
        /// Returns a content item. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        Task<DeliveryItemResponse> GetItemAsync(string codename, IEnumerable<IQueryParameter> parameters);

        /// <summary>
        /// Returns a strongly typed content item. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null);

        /// <summary>
        /// Returns content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        Task<DeliveryItemListingResponse> GetItemsAsync(params IQueryParameter[] parameters);

        /// <summary>
        /// Returns content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        Task<DeliveryItemListingResponse> GetItemsAsync(IEnumerable<IQueryParameter> parameters);

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(params IQueryParameter[] parameters);

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters);

        /// <summary>
        /// Returns a feed that is used to traverse through content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed"/> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        IDeliveryItemsFeed GetItemsFeed(params IQueryParameter[] parameters);

        /// <summary>
        /// Returns a feed that is used to traverse through content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed"/> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        IDeliveryItemsFeed GetItemsFeed(IEnumerable<IQueryParameter> parameters);

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed content items matching the optional filtering parameters.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{T}"/> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        IDeliveryItemsFeed<T> GetItemsFeed<T>(params IQueryParameter[] parameters);

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed content items matching the optional filtering parameters.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{T}"/> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        IDeliveryItemsFeed<T> GetItemsFeed<T>(IEnumerable<IQueryParameter> parameters);

        /// <summary>
        /// Returns a content type as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content type with the specified codename.</returns>
        Task<JObject> GetTypeJsonAsync(string codename);

        /// <summary>
        /// Returns content types as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        Task<JObject> GetTypesJsonAsync(params string[] parameters);

        /// <summary>
        /// Returns a content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="DeliveryTypeResponse"/> instance that contains the content type with the specified codename.</returns>
        Task<DeliveryTypeResponse> GetTypeAsync(string codename);

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        Task<DeliveryTypeListingResponse> GetTypesAsync(params IQueryParameter[] parameters);

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        Task<DeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter> parameters);

        /// <summary>
        /// Returns a content type element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content type element.</param>
        /// <returns>The <see cref="DeliveryElementResponse"/> instance that contains the specified content type element.</returns>
        Task<DeliveryElementResponse> GetContentElementAsync(string contentTypeCodename, string contentElementCodename);

        /// <summary>
        /// Returns a taxonomy group as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the taxonomy group with the specified codename.</returns>
        Task<JObject> GetTaxonomyJsonAsync(string codename);

        /// <summary>
        /// Returns taxonomy groups as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        Task<JObject> GetTaxonomiesJsonAsync(params string[] parameters);

        /// <summary>
        /// Returns a taxonomy group.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="DeliveryTaxonomyResponse"/> instance that contains the taxonomy group with the specified codename.</returns>
        Task<DeliveryTaxonomyResponse> GetTaxonomyAsync(string codename);

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(params IQueryParameter[] parameters);

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(IEnumerable<IQueryParameter> parameters);
    }
}