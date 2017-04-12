using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a list of content items.
    /// </summary>
    public sealed class DeliveryItemListingResponse
    {
        private readonly JToken _response;
        private readonly DeliveryClient _client;
        private Pagination _pagination;
        private IReadOnlyList<ContentItem> _items;
        private dynamic _modularContent;

        /// <summary>
        /// Gets paging information.
        /// </summary>
        public Pagination Pagination
        {
            get { return _pagination ?? (_pagination = new Pagination(_response["pagination"])); }
        }

        /// <summary>
        /// Gets a list of content items.
        /// </summary>
        public IReadOnlyList<ContentItem> Items
        {
            get { return _items ?? (_items = ((JArray)_response["items"]).Select(source => new ContentItem(source, _response["modular_content"], _client)).ToList().AsReadOnly()); }
        }

        /// <summary>
        /// Gets the dynamic view of the JSON response where modular content items and their properties can be retrieved by name, for example <c>ModularContent.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic ModularContent
        {
            get { return _modularContent ?? (_modularContent = JObject.Parse(_response["modular_content"].ToString())); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse"/> class with information from a response.
        /// </summary>
        /// <param name="response">A response from Kentico Cloud Delivery API that contains a list of content items.</param>
        internal DeliveryItemListingResponse(JToken response, DeliveryClient client)
        {
            _response = response;
            _client = client;

        }

        /// <summary>
        /// Casts DeliveryItemListingResponse to it's generic version.
        /// </summary>
        /// <typeparam name="T">Generic version type.</typeparam>
        public DeliveryItemListingResponse<T> CastTo<T>()
        {
            return new DeliveryItemListingResponse<T>(_response, _client);
        }
    }
}
