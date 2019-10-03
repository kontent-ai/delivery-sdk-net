using System;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a content item.
    /// </summary>
    public sealed class DeliveryItemResponse : AbstractResponse
    {
        private readonly IModelProvider _modelProvider;
        private readonly IContentLinkUrlResolver _contentLinkUrlResolver;
        private readonly Lazy<ContentItem> _item;
        private readonly Lazy<JObject> _linkedItems;

        /// <summary>
        /// Gets the content item.
        /// </summary>
        public ContentItem Item => _item.Value;

        /// <summary>
        /// Gets the dynamic view of the JSON response where linked items and their properties can be retrieved by name, for example <c>LinkedItems.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        /// <param name="contentLinkUrlResolver">The resolver that can generate URLs for links in rich text elements.</param>
        internal DeliveryItemResponse(ApiResponse response, IModelProvider modelProvider, IContentLinkUrlResolver contentLinkUrlResolver) : base(response)
        {
            _modelProvider = modelProvider;
            _contentLinkUrlResolver = contentLinkUrlResolver;
            _item = new Lazy<ContentItem>(() => new ContentItem(_response.Content["item"], _response.Content["modular_content"], _contentLinkUrlResolver, _modelProvider), LazyThreadSafetyMode.PublicationOnly);
            _linkedItems = new Lazy<JObject>(() => (JObject)_response.Content["modular_content"].DeepClone(), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Casts this response to a generic one.
        /// </summary>
        /// <typeparam name="T">The object type that the item will be deserialized to.</typeparam>
        public DeliveryItemResponse<T> CastTo<T>()
        {
            return new DeliveryItemResponse<T>(_response, _modelProvider);
        }

        /// <summary>
        /// Implicitly converts the specified <paramref name="response"/> to a content item.
        /// </summary>
        /// <param name="response">The response to convert.</param>
        public static implicit operator ContentItem(DeliveryItemResponse response) => response.Item;
    }
}
