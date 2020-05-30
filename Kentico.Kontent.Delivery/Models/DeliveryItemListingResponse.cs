using Kentico.Kontent.Delivery.Abstractions.Responses;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions.Models;
using Kentico.Kontent.Delivery.Abstractions.StrongTyping;

namespace Kentico.Kontent.Delivery.Models
{
    /// <inheritdoc cref="IDeliveryItemListingResponse{T}" />
    public sealed class DeliveryItemListingResponse<T> : AbstractResponse, IDeliveryItemListingResponse<T>
    {
        private readonly IModelProvider _modelProvider;
        private readonly Lazy<Pagination> _pagination;
        private readonly Lazy<IReadOnlyList<T>> _items;
        private readonly Lazy<JObject> _linkedItems;

        /// <inheritdoc/>
        public IPagination Pagination => _pagination.Value;

        /// <inheritdoc/>
        public IReadOnlyList<T> Items => _items.Value;

        /// <inheritdoc/>
        public dynamic LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of content items.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        internal DeliveryItemListingResponse(ApiResponse response, IModelProvider modelProvider) : base(response)
        {
            _modelProvider = modelProvider;
            _pagination = new Lazy<Pagination>(() => response.JsonContent["pagination"].ToObject<Pagination>(), LazyThreadSafetyMode.PublicationOnly);
            _items = new Lazy<IReadOnlyList<T>>(() => ((JArray)response.JsonContent["items"]).Select(source => _modelProvider.GetContentItemModel<T>(source, response.JsonContent["modular_content"])).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
            _linkedItems = new Lazy<JObject>(() => (JObject)response.JsonContent["modular_content"].DeepClone(), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
