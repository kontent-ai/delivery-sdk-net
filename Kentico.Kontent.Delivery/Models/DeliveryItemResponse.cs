using System;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions.Responses;
using Kentico.Kontent.Delivery.Abstractions.StrongTyping;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.Models
{
    /// <inheritdoc/>
    public sealed class DeliveryItemResponse<T> : AbstractResponse, IDeliveryItemResponse<T>
    {
        private readonly Lazy<T> _item;
        private readonly Lazy<JObject> _linkedItems;

        /// <inheritdoc/>
        public T Item => _item.Value;

        /// <inheritdoc/>
        public dynamic LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        internal DeliveryItemResponse(ApiResponse response, IModelProvider modelProvider) : base(response)
        {
            _item = new Lazy<T>(() => modelProvider.GetContentItemModel<T>(response.JsonContent["item"], response.JsonContent["modular_content"]), LazyThreadSafetyMode.PublicationOnly);
            _linkedItems = new Lazy<JObject>(() => (JObject)response.JsonContent["modular_content"].DeepClone(), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Implicitly converts the specified <paramref name="response"/> to a content item.
        /// </summary>
        /// <param name="response">The response to convert.</param>
        public static implicit operator T(DeliveryItemResponse<T> response) => response.Item;
    }
}
