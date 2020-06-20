using System;
using System.Collections.Generic;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemResponse{T}" />
    internal sealed class DeliveryItemResponse<T> : AbstractItemsResponse, IDeliveryItemResponse<T>
    {
        private Lazy<T> _item;

        /// <inheritdoc/>
        public T Item
        {
            get => _item.Value;
            private set => _item = new Lazy<T>(() => value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        internal DeliveryItemResponse(ApiResponse response, IModelProvider modelProvider) : base(response, modelProvider)
        {
            _item = new Lazy<T>(() => modelProvider.GetContentItemModel<T>(response.JsonContent["item"], response.JsonContent["modular_content"]), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        /// <param name="item">Content item of a specific type.</param>
        /// <param name="linkedItems">Collection of linked content items.</param>
        [JsonConstructor]
        internal DeliveryItemResponse(ApiResponse response, T item, IReadOnlyList<object> linkedItems) : base(response, linkedItems)
        {
            Item = item;
        }

        /// <summary>
        /// Implicitly converts the specified <paramref name="response"/> to a content item.
        /// </summary>
        /// <param name="response">The response to convert.</param>
        public static implicit operator T(DeliveryItemResponse<T> response) => response.Item;
    }
}
