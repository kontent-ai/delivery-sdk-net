using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">Generic strong type of item representation.</typeparam>
    public sealed class DeliveryItemListingResponse<T> : AbstractResponse
    {
        private readonly ApiResponse _response;
        private readonly IModelProvider _modelProvider;
        private dynamic _linkedItems;
        private Pagination _pagination;
        private IReadOnlyList<T> _items;

        /// <summary>
        /// Gets paging information.
        /// </summary>
        public Pagination Pagination
        {
            get { return _pagination ?? (_pagination = _response.Content["pagination"].ToObject<Pagination>()); }
        }

        /// <summary>
        /// Gets a list of content items.
        /// </summary>
        public IReadOnlyList<T> Items
        {
            get { return _items ?? (_items = ((JArray)_response.Content["items"]).Select(source => _modelProvider.GetContentItemModel<T>(source, _response.Content["modular_content"])).ToList().AsReadOnly()); }
        }


        /// <summary>
        /// Gets the dynamic view of the JSON response where linked items and their properties can be retrieved by name, for example <c>LinkedItems.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic LinkedItems
        {
            get { return _linkedItems ?? (_linkedItems = JObject.Parse(_response.Content["modular_content"].ToString())); }
        }

        /// <summary>
        /// Gets a value that determines if content is stale.
        /// Stale content indicates that there is a more recent version, but it will become available later.
        /// Stale content should be cached only for a limited period of time.
        /// </summary>
        public bool HasStaleContent
        {
            get { return _response.HasStaleContent; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse"/> class with information from a response.
        /// </summary>
        /// <param name="response">A response from Kentico Cloud Delivery API that contains a list of content items.</param>
        /// <param name="modelProvider"></param>
        /// <param name="apiUrl">API URL used to communicate with the underlying Kentico Cloud endpoint.</param>
        internal DeliveryItemListingResponse(ApiResponse response, IModelProvider modelProvider, string apiUrl) : base(apiUrl)
        {
            _response = response;
            _modelProvider = modelProvider;
        }
    }
}
