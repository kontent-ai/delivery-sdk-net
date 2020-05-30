using System;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;
using Kentico.Kontent.Delivery.SharedModels;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemResponse{T}" />
    public sealed class DeliveryItemResponse<T> : AbstractItemsResponse, IDeliveryItemResponse<T>
    {
        private readonly Lazy<T> _item;

        /// <inheritdoc/>
        public T Item => _item.Value;

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
        /// Implicitly converts the specified <paramref name="response"/> to a content item.
        /// </summary>
        /// <param name="response">The response to convert.</param>
        public static implicit operator T(DeliveryItemResponse<T> response) => response.Item;
    }
}
