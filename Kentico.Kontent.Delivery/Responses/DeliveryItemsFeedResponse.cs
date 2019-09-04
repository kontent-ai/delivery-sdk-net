using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a partial response from Kentico Kontent Delivery API enumeration methods that contains a list of content items.
    /// </summary>
    public class DeliveryItemsFeedResponse : FeedResponse, IEnumerable<ContentItem>
    {
        private readonly IModelProvider _modelProvider;
        private readonly Lazy<IReadOnlyList<ContentItem>> _items;
        private readonly Lazy<JObject> _linkedItems;

        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        public IReadOnlyList<ContentItem> Items => _items.Value;

        /// <summary>
        /// Gets the dynamic view of the JSON response where linked items and their properties can be retrieved by name, for example <c>LinkedItems.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemsFeedResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of content items.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        /// <param name="contentLinkUrlResolver">The resolver that can generate URLs for links in rich text elements.</param>
        internal DeliveryItemsFeedResponse(ApiResponse response, IModelProvider modelProvider, IContentLinkUrlResolver contentLinkUrlResolver) : base(response)
        {
            _modelProvider = modelProvider;
            _items = new Lazy<IReadOnlyList<ContentItem>>(() => ((JArray)_response.Content["items"])
                .Select(source => new ContentItem(source, _response.Content["modular_content"], contentLinkUrlResolver, _modelProvider))
                .ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
            _linkedItems = new Lazy<JObject>(() => (JObject)_response.Content["modular_content"].DeepClone(), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of content items.
        /// </summary>
        public IEnumerator<ContentItem> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of content items.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Casts this response to a generic one. To succeed all items must be of the same type.
        /// </summary>
        /// <typeparam name="T">The object type that the items will be deserialized to.</typeparam>
        public DeliveryItemsFeedResponse<T> CastTo<T>()
        {
            return new DeliveryItemsFeedResponse<T>(_response, _modelProvider);
        }
    }
}