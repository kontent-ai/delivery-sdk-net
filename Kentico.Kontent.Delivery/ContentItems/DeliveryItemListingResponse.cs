using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemListingResponse{T}" />
    public sealed class DeliveryItemListingResponse<T> : AbstractItemsResponse, IDeliveryItemListingResponse<T>
    {
        private readonly Lazy<Pagination> _pagination;
        private readonly Lazy<IReadOnlyList<T>> _items;

        /// <inheritdoc/>
        public IPagination Pagination => _pagination.Value;

        /// <inheritdoc/>
        public IReadOnlyList<T> Items => _items.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of content items.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        internal DeliveryItemListingResponse(ApiResponse response, IModelProvider modelProvider) : base(response, modelProvider)
        {
            _pagination = new Lazy<Pagination>(() => response.JsonContent["pagination"].ToObject<Pagination>(), LazyThreadSafetyMode.PublicationOnly);
            _items = new Lazy<IReadOnlyList<T>>(() => ((JArray)response.JsonContent["items"]).Select(source => modelProvider.GetContentItemModel<T>(source, response.JsonContent["modular_content"])).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
