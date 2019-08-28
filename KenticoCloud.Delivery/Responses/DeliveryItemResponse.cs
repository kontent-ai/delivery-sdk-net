using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a content item.
    /// </summary>
    public sealed class DeliveryItemResponse : AbstractResponse
    {
        private readonly ApiResponse _response;
        private readonly IModelProvider _modelProvider;
        private readonly IContentLinkUrlResolver _contentLinkUrlResolver;
        private dynamic _linkedItems;
        private ContentItem _item;

        /// <summary>
        /// Gets the content item from the response.
        /// </summary>
        public ContentItem Item
        {
            get { return _item ?? (_item = new ContentItem(_response.Content["item"], _response.Content["modular_content"], _contentLinkUrlResolver, _modelProvider)); }
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
        /// Initializes a new instance of the <see cref="DeliveryItemResponse"/> class with information from a response.
        /// </summary>
        /// <param name="response">A response from Kentico Cloud Delivery API that contains a content item.</param>
        /// /// <param name="modelProvider">An instance of an object that can JSON responses into strongly typed CLR objects</param>
        /// <param name="contentLinkUrlResolver">An instance of an object that can resolve links in rich text elements</param>
        /// <param name="apiUrl">API URL used to communicate with the underlying Kentico Cloud endpoint.</param>
        internal DeliveryItemResponse(ApiResponse response, IModelProvider modelProvider, IContentLinkUrlResolver contentLinkUrlResolver, string apiUrl) : base(apiUrl)
        {
            _response = response;
            _modelProvider = modelProvider;
            _contentLinkUrlResolver = contentLinkUrlResolver;
        }

        /// <summary>
        /// Casts DeliveryItemResponse to its generic version.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        public DeliveryItemResponse<T> CastTo<T>()
        {
            return new DeliveryItemResponse<T>(_response, _modelProvider, ApiUrl);
        }
    }
}
