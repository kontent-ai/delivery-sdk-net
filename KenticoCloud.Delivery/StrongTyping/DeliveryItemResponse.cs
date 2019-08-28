using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">Generic strong type of item representation.</typeparam>
    public sealed class DeliveryItemResponse<T> : AbstractResponse
    {
        private readonly ApiResponse _response;
        private readonly IModelProvider _modelProvider;
        private dynamic _linkedItems;
        private T _item;

        /// <summary>
        /// Gets a content item.
        /// </summary>
        public T Item
        {
            get
            {
                if (_item == null)
                {
                    _item = _modelProvider.GetContentItemModel<T>(_response.Content["item"], _response.Content["modular_content"]);
                }
                return _item;
            }
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
            get
            {
                return _response.HasStaleContent;
            }
        }

        internal DeliveryItemResponse(ApiResponse response, IModelProvider modelProvider, string apiUrl) : base(apiUrl)
        {
            _response = response;
            _modelProvider = modelProvider;
        }
    }
}
