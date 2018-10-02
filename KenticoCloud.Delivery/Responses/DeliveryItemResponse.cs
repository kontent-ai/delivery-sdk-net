using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a content item.
    /// </summary>
    public sealed class DeliveryItemResponse : AbstractResponse
    {
        private readonly JToken _response;
        private readonly ICodeFirstModelProvider _codeFirstModelProvider;
        private readonly IContentLinkUrlResolver _contentLinkUrlResolver;
        private dynamic _modularContent;
        private ContentItem _item;

        /// <summary>
        /// Gets the content item from the response.
        /// </summary>
        public ContentItem Item
        {
            get { return _item ?? (_item = new ContentItem(_response["item"], _response["modular_content"], _contentLinkUrlResolver, _codeFirstModelProvider)); }
        }

        /// <summary>
        /// Gets the dynamic view of the JSON response where modular content items and their properties can be retrieved by name, for example <c>ModularContent.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic ModularContent
        {
            get { return _modularContent ?? (_modularContent = JObject.Parse(_response["modular_content"].ToString())); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse"/> class with information from a response.
        /// </summary>
        /// <param name="response">A response from Kentico Cloud Delivery API that contains a content item.</param>
        /// <param name="client">The client that retrieved the content item.</param>
        /// <param name="apiUrl">API URL used to communicate with the underlying Kentico Cloud endpoint.</param>
        internal DeliveryItemResponse(JToken response, ICodeFirstModelProvider codeFirstModelProvider, IContentLinkUrlResolver contentLinkUrlResolver, string apiUrl) : base(apiUrl)
        {
            _response = response;
            _codeFirstModelProvider = codeFirstModelProvider;
            _contentLinkUrlResolver = contentLinkUrlResolver;
        }

        /// <summary>
        /// Casts DeliveryItemResponse to its generic version.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        public DeliveryItemResponse<T> CastTo<T>()
        {
            return new DeliveryItemResponse<T>(_response, _codeFirstModelProvider, ApiUrl);
        }
    }
}
