using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a list of content items.
    /// </summary>
    public sealed class DeliveryItemListingResponse : AbstractResponse
    {
        private readonly IModelProvider _modelProvider;
        private readonly IContentLinkUrlResolver _contentLinkUrlResolver;
        private readonly Lazy<Pagination> _pagination;
        private readonly Lazy<IReadOnlyList<ContentItem>> _items;
        private readonly Lazy<JObject> _linkedItems;

        /// <summary>
        /// Gets paging information.
        /// </summary>
        public Pagination Pagination => _pagination.Value;

        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        public IReadOnlyList<ContentItem> Items => _items.Value;

        /// <summary>
        /// Gets the dynamic view of the JSON response where linked items and their properties can be retrieved by name, for example <c>LinkedItems.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Cloud Delivery API that contains a list of content items.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        /// <param name="contentLinkUrlResolver">The resolver that can generate URLs for links in rich text elements.</param>
        internal DeliveryItemListingResponse(ApiResponse response, IModelProvider modelProvider, IContentLinkUrlResolver contentLinkUrlResolver) : base(response)
        {
            _modelProvider = modelProvider;
            _contentLinkUrlResolver = contentLinkUrlResolver;
            _pagination = new Lazy<Pagination>(() => _response.Content["pagination"].ToObject<Pagination>(), LazyThreadSafetyMode.PublicationOnly);
            _items = new Lazy<IReadOnlyList<ContentItem>>(() => ((JArray)_response.Content["items"]).Select(source => new ContentItem(source, _response.Content["modular_content"], _contentLinkUrlResolver, _modelProvider)).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
            _linkedItems = new Lazy<JObject>(() => (JObject)_response.Content["modular_content"].DeepClone(), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Casts this response to its generic version. To succceed all items must be of the same type.
        /// </summary>
        /// <typeparam name="T">The object type that the items will be deserialized to.</typeparam>
        public DeliveryItemListingResponse<T> CastTo<T>()
        {
            return new DeliveryItemListingResponse<T>(_response, _modelProvider);
        }
    }
}
