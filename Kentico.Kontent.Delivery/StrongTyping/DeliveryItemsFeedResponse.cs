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
    /// <typeparam name="T">The type of content items in the response.</typeparam>
    public class DeliveryItemsFeedResponse<T> : FeedResponse, IEnumerable<T>
    {
        private readonly Lazy<IReadOnlyList<T>> _items;
        private readonly Lazy<JObject> _linkedItems;

        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        public IReadOnlyList<T> Items => _items.Value;

        /// <summary>
        /// Gets the dynamic view of the JSON response where linked items and their properties can be retrieved by name, for example <c>LinkedItems.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemsFeedResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of content items.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        internal DeliveryItemsFeedResponse(ApiResponse response, IModelProvider modelProvider) : base(response)
        {
            _items = new Lazy<IReadOnlyList<T>>(() => ((JArray)_response.Content["items"]).Select(source => modelProvider.GetContentItemModel<T>(source, _response.Content["modular_content"])).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
            _linkedItems = new Lazy<JObject>(() => (JObject)_response.Content["modular_content"].DeepClone(), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of content items.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
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
    }
}