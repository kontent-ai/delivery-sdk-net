using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemListingResponse{T}" />
    internal sealed class DeliveryItemListingResponse<T> : AbstractItemsResponse, IDeliveryItemListingResponse<T>
    {
        private Lazy<IPagination> _pagination;
        private Lazy<IReadOnlyList<T>> _items;

        /// <inheritdoc/>
        public IPagination Pagination
        {
            get => _pagination.Value;
            private set => _pagination = new Lazy<IPagination>(() => value);
        }

        /// <inheritdoc/>
        public IReadOnlyList<T> Items
        {
            get => _items.Value;
            private set => _items = new Lazy<IReadOnlyList<T>>(() => value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of content items.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        internal DeliveryItemListingResponse(ApiResponse response, IModelProvider modelProvider) : base(response, modelProvider)
        {
            _pagination = new Lazy<IPagination>(() => response.JsonContent["pagination"].ToObject<Pagination>(), LazyThreadSafetyMode.PublicationOnly);
            _items = new Lazy<IReadOnlyList<T>>(() => ((JArray)response.JsonContent["items"]).Select(source => modelProvider.GetContentItemModel<T>(source, response.JsonContent["modular_content"])).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains content items.</param>
        /// <param name="items">A collection of content items of a specific type.</param>
        /// <param name="linkedItems">Collection of linked content items.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryItemListingResponse(ApiResponse response, IReadOnlyList<T> items, IReadOnlyList<object> linkedItems, IPagination pagination) : base(response, linkedItems)
        {
            Items = items;
            Pagination = pagination;
        }
    }
}
