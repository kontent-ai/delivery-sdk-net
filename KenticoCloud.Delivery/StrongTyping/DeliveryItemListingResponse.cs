using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">The type of content items in the response.</typeparam>
    public sealed class DeliveryItemListingResponse<T> : AbstractResponse
    {
        private readonly IModelProvider _modelProvider;
        private readonly Lazy<Pagination> _pagination;
        private readonly Lazy<IReadOnlyList<T>> _items;
        private readonly Lazy<JObject> _linkedItems;

        /// <summary>
        /// Gets paging information.
        /// </summary>
        public Pagination Pagination => _pagination.Value;

        /// <summary>
        /// Gets a read-only list of content items.
        /// </summary>
        public IReadOnlyList<T> Items => _items.Value;

        /// <summary>
        /// Gets the dynamic view of the JSON response where linked items and their properties can be retrieved by name, for example <c>LinkedItems.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Cloud Delivery API that contains a list of content items.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        internal DeliveryItemListingResponse(ApiResponse response, IModelProvider modelProvider) : base(response)
        {
            _modelProvider = modelProvider;
            _pagination = new Lazy<Pagination>(() => _response.Content["pagination"].ToObject<Pagination>(), LazyThreadSafetyMode.PublicationOnly);
            _items = new Lazy<IReadOnlyList<T>>(() => ((JArray)_response.Content["items"]).Select(source => _modelProvider.GetContentItemModel<T>(source, _response.Content["modular_content"])).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
            _linkedItems = new Lazy<JObject>(() => (JObject)_response.Content["modular_content"].DeepClone(), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
