using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Extra overloads of the <see cref="IDeliveryClient"/> for the consumer's convenience. 
    /// </summary>
    public static class DeliveryClientExtensions
    {
        /// <summary>
        /// Gets a strongly typed content item by its codename. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="client">An instance of the <see cref="IDeliveryClient"/></param>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="IDeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public static Task<IDeliveryItemResponse<T>> GetItemAsync<T>(this IDeliveryClient client, string codename, params IQueryParameter[] parameters)
        {
            return client.GetItemAsync<T>(codename, parameters);
        }

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="client">An instance of the <see cref="IDeliveryClient"/></param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="IDeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public static Task<IDeliveryItemListingResponse<T>> GetItemsAsync<T>(this IDeliveryClient client, params IQueryParameter[] parameters)
        {
            return client.GetItemsAsync<T>(parameters);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed content items matching the optional filtering parameters.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="client">An instance of the <see cref="IDeliveryClient"/></param>
        /// <param name="parameters">An array of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{T}"/> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        public static IDeliveryItemsFeed<T> GetItemsFeed<T>(this IDeliveryClient client, params IQueryParameter[] parameters)
        {
            return client.GetItemsFeed<T>(parameters);
        }

        /// <summary>
        /// Returns content types that match the optional filtering parameters.
        /// </summary>
        /// <param name="client">An instance of the <see cref="IDeliveryClient"/></param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryTypeListingResponse"/> instance that contains the content types. If no query parameters are specified, all content types are returned.</returns>
        public static Task<IDeliveryTypeListingResponse> GetTypesAsync(this IDeliveryClient client, params IQueryParameter[] parameters)
        {
            return client.GetTypesAsync(parameters);
        }

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="client">An instance of the <see cref="IDeliveryClient"/></param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public static Task<IDeliveryTaxonomyListingResponse> GetTaxonomiesAsync(this IDeliveryClient client, params IQueryParameter[] parameters)
        {
            return client.GetTaxonomiesAsync(parameters);
        }

        /// <summary>
        /// Returns all active languages assigned to a given environment and matching the optional filtering parameters.
        /// </summary>
        /// <param name="client">An instance of the <see cref="IDeliveryClient"/></param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="IDeliveryLanguageListingResponse"/> instance that represents the languages. If no query parameters are specified, all languages are returned.</returns>
        public static Task<IDeliveryLanguageListingResponse> GetLanguagesAsync(this IDeliveryClient client, params IQueryParameter[] parameters)
        {
            return client.GetLanguagesAsync(parameters);
        }

        /// <summary>
        /// Initializes synchronization of changes in content items based on the specified parameters. After the initialization, you'll get an X-Continuation token in the response.
        /// </summary>
        /// <param name="client">An instance of the <see cref="IDeliveryClient"/></param>
        /// <param name="parameters">A collection of query parameters, for example, for filtering.</param>
        /// <returns>The <see cref="IDeliverySyncInitResponse"/> instance that represents the sync init response that contains continuation token needed for further sync execution.</returns>
        public static Task<IDeliverySyncInitResponse> PostSyncInitAsync(this IDeliveryClient client, params IQueryParameter[] parameters)
        {
            return client.PostSyncInitAsync(parameters);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed parent content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="client">An instance of the <see cref="IDeliveryClient"/></param>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{IUsedInItem}"/> instance that can be used to enumerate through content item parents for the specified item codename. If no query parameters are specified, default language parents are enumerated.</returns>
        public static IDeliveryItemsFeed<IUsedInItem> GetItemUsedIn(this IDeliveryClient client, string codename, params IQueryParameter[] parameters)
        {
            return client.GetItemUsedIn(codename, parameters);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed parent content items matching the optional filtering parameters.
        /// </summary>
        /// <param name="client">An instance of the <see cref="IDeliveryClient"/></param>
        /// <param name="codename">The codename of an asset.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, or ordering.</param>
        /// <returns>The <see cref="IDeliveryItemsFeed{IUsedInItem}"/> instance that can be used to enumerate through asset parents for the specified asset codename. If no query parameters are specified, default language parents are enumerated.</returns>
        public static IDeliveryItemsFeed<IUsedInItem> GetAssetUsedIn(this IDeliveryClient client, string codename, params IQueryParameter[] parameters)
        {
            return client.GetAssetUsedIn(codename, parameters);
        }
    }
}
